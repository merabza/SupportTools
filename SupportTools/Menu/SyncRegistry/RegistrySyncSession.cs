using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.SyncRegistry;

//ერთი სინქრონიზაცია (SyncRegistryCliMenuCommand-ის მესამე ნაბიჯი):
//- გეგმა, ადაპტერების გაფრთხილებები და PathMapper-ის issue-ები. ისინი სინქრონიზაციას არ აჩერებს (მომხმარებლის
//  გადაწყვეტილება): მომხმარებელი ირჩევს, გააგრძელოს, ჩანაწერი გამორიცხოს თუ გააუქმოს;
//- შეჯამება კოლექციების მიხედვით და არჩევანი: Apply (არაკონფლიქტური Pull და Push), Pull only, Push only,
//  კონფლიქტების გადაწყვეტა (Local / Server / Skip, ველების დიფით), ჯგუფური „Local wins“ პირველი სინქრონიზაციის
//  კონფლიქტებისთვის (seed), დეტალები, Dry run (აჩვენებს, რა გაიგზავნება, და არაფერს აგზავნის), ჩანაწერის გამორიცხვა
//  ამ კომპიუტერზე (ExcludedKeys) და Cancel. გადაწყვეტილი კონფლიქტი მიმართულებით სრულდება: Apply ყველას ასრულებს,
//  Pull only მხოლოდ სერვერის სასარგებლოდ გადაწყვეტილ კონფლიქტებს, Push only კი ლოკალურის სასარგებლოდ გადაწყვეტილს;
//- შესრულება პროგრესით. სერვერის პირველი წარუმატებელი ოპერაციის შემდეგ სერვერის დანარჩენი ოპერაციები ჩერდება
//  (StopOnFailure, მომხმარებლის გადაწყვეტილება): ასობით მოთხოვნიდან (seed) ჩავარდნილი ჩანაწერი სახელით ჩანს;
//- ანგარიში. Pull-ის წინა პარამეტრების ფაილს ParametersManager.Save ინახავს .bak ასლად (A4), ამიტომ დამატებითი ასლი
//  აღარ კეთდება.
//საიდუმლოებები არსად იბეჭდება: ჩანაწერები სახელით ჩანს, ველები კი RegistryRecordDiff-ით, დაფარული მნიშვნელობებით
internal sealed class RegistrySyncSession
{
    private const int LocalResolutionId = 0;
    private const int ServerResolutionId = 1;
    private const int ShowDifferencesId = 3;

    //ანგარიშის რაოდენობების რიგი; None (InSync და Skipped) არ ჩანს
    private static readonly ERegistrySyncOutcome[] ReportedOutcomes =
    [
        ERegistrySyncOutcome.Done, ERegistrySyncOutcome.Conflict, ERegistrySyncOutcome.Failed,
        ERegistrySyncOutcome.NotExecuted, ERegistrySyncOutcome.NotSelected
    ];

    private readonly RegistrySyncEngine _engine;
    private readonly Func<string, CliMenuSet, int> _inputIdFromMenuList;
    private readonly ILogger _logger;
    private readonly SupportToolsParameters _parameters;
    private readonly IParametersManager _parametersManager;
    private readonly PathMapper _pathMapper;

    //კონფლიქტების გადაწყვეტა „კოლექცია/გასაღები“-ით, რომ ჩანაწერის გამორიცხვის შემდეგ აგებულ ახალ გეგმასაც მიესადაგოს.
    //გამოტოვებული (Skip) კონფლიქტი აქ არ არის
    private readonly Dictionary<string, ERegistryConflictResolution> _resolutions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly RegistrySyncWarnings _warnings;
    private int _shownPathIssuesCount;
    private int _shownWarningsCount;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RegistrySyncSession(RegistrySyncEngine engine, RegistrySyncWarnings warnings, PathMapper pathMapper,
        IParametersManager parametersManager, Func<string, CliMenuSet, int> inputIdFromMenuList, ILogger logger)
    {
        _engine = engine;
        _warnings = warnings;
        _pathMapper = pathMapper;
        _parametersManager = parametersManager;
        _parameters = (SupportToolsParameters)parametersManager.Parameters;
        _inputIdFromMenuList = inputIdFromMenuList;
        _logger = logger;
    }

    private enum ESyncChoice
    {
        Apply,
        PullOnly,
        PushOnly,
        ResolveConflicts,
        LocalWinsFirstSyncConflicts,
        ShowDetails,
        DryRun,
        ExcludeRecord,
        Cancel
    }

    public async Task<bool> Run(CancellationToken cancellationToken)
    {
        RegistrySyncPlan? plan = await CreatePlan(cancellationToken);
        while (plan is not null && HasChanges(plan))
        {
            switch (InputChoice(plan))
            {
                case ESyncChoice.Apply:
                    return await Execute(plan, true, true, cancellationToken);
                case ESyncChoice.PullOnly:
                    return await Execute(plan, true, false, cancellationToken);
                case ESyncChoice.PushOnly:
                    return await Execute(plan, false, true, cancellationToken);
                case ESyncChoice.ResolveConflicts:
                    ResolveConflicts(plan);
                    break;
                case ESyncChoice.LocalWinsFirstSyncConflicts:
                    ResolveFirstSyncConflictsAsLocal(plan);
                    break;
                case ESyncChoice.ShowDetails:
                    WriteDetails(plan);
                    break;
                case ESyncChoice.DryRun:
                    WriteDryRun(plan);
                    break;
                case ESyncChoice.ExcludeRecord:
                    plan = await ExcludeRecord(plan, cancellationToken);
                    break;
                default:
                    Console.WriteLine("Canceled, nothing was synced");
                    return true;
            }
        }

        return plan is not null && await RecordInSyncState(plan, cancellationToken);
    }

    //გეგმა, ახალი გაფრთხილებები და შეჯამება. სერვერის წაკითხვის შეცდომა (მაგ. სერვერი შუალედში გაითიშა) აჩერებს
    private async Task<RegistrySyncPlan?> CreatePlan(CancellationToken cancellationToken)
    {
        Result<RegistrySyncPlan> plan = await _engine.CreatePlan(cancellationToken);
        if (plan.IsFailure)
        {
            WriteError($"The registry sync plan was not created: {plan.Error.Description}");
            return null;
        }

        WriteNewWarnings();
        Console.WriteLine("Registry sync plan:");
        foreach (string line in RegistrySyncPlanSummary.CreateLines(plan.Value))
        {
            Console.WriteLine(line);
        }

        return plan.Value;
    }

    private static bool HasChanges(RegistrySyncPlan plan)
    {
        return plan.Items.Any(IsChange);
    }

    //ჩანაწერი, რომელზეც სინქრონიზაციას რამე აქვს გასაკეთებელი ან მომხმარებლის გადაწყვეტილებას ელოდება
    private static bool IsChange(RegistrySyncPlanItem item)
    {
        return item.Action is ERegistrySyncAction.Pull or ERegistrySyncAction.Push or ERegistrySyncAction.Conflict;
    }

    //გადასატანი არაფერია, მაგრამ შესრულება მაინც საჭიროა: პირველი სინქრონიზაციისას ერთნაირი ჩანაწერების მდგომარეობა
    //იწერება, თორემ მათი შემდეგი ცვლილება FirstSyncDiffers კონფლიქტად გამოჩნდებოდა
    private async Task<bool> RecordInSyncState(RegistrySyncPlan plan, CancellationToken cancellationToken)
    {
        RegistrySyncReport report =
            await _engine.Execute(plan, RegistrySyncSelection.AllNonConflicting, null, cancellationToken);
        Console.WriteLine("The local registry and SupportToolsServer are in sync, nothing to send or to take");
        if (!report.Changed || report.Saved)
        {
            return true;
        }

        WriteError("The registry sync state was not saved");
        return false;
    }

    //ადაპტერების გაფრთხილებები და გზები, რომლებიც კანონიკურ ან ლოკალურ ფორმაში ვერ გარდაიქმნა; თითო მხოლოდ ერთხელ
    private void WriteNewWarnings()
    {
        foreach (RegistrySyncWarning warning in _warnings.Items.Skip(_shownWarningsCount))
        {
            string record = warning.Key is null ? warning.CollectionName : $"{warning.CollectionName}/{warning.Key}";
            StShared.WriteWarningLine($"{record}: {warning.Message}", true, _logger);
        }

        _shownWarningsCount = _warnings.Items.Count;

        foreach (PathMappingIssue issue in _pathMapper.Issues.Skip(_shownPathIssuesCount))
        {
            StShared.WriteWarningLine(issue.Direction == EPathMappingDirection.ToCanonical
                ? $"The path {issue.Path} has no canonical form: add a path mapping in Support Tools Parameters Editor"
                : $"The server path {issue.Path} has no form on this computer: add a path mapping in Support Tools " +
                  "Parameters Editor", true, _logger);
        }

        _shownPathIssuesCount = _pathMapper.Issues.Count;
    }

    private ESyncChoice InputChoice(RegistrySyncPlan plan)
    {
        List<(ESyncChoice Choice, string Name, string Status)> choices = CreateChoices(plan);
        var menuSet = new CliMenuSet();
        foreach ((ESyncChoice Choice, string Name, string Status) choice in choices)
        {
            menuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(choice.Name, choice.Status));
        }

        int id = _inputIdFromMenuList("action", menuSet);
        return id >= 0 && id < choices.Count ? choices[id].Choice : ESyncChoice.Cancel;
    }

    private List<(ESyncChoice Choice, string Name, string Status)> CreateChoices(RegistrySyncPlan plan)
    {
        List<RegistrySyncPlanItem> conflicts = GetConflicts(plan);
        int firstSyncConflictsCount = conflicts.Count(x => x.Conflict == ERegistrySyncConflict.FirstSyncDiffers);
        List<(ESyncChoice Choice, string Name, string Status)> choices =
        [
            (ESyncChoice.Apply, "Apply", DescribeOperations(plan, true, true)),
            (ESyncChoice.PullOnly, "Pull only", DescribeOperations(plan, true, false)),
            (ESyncChoice.PushOnly, "Push only", DescribeOperations(plan, false, true))
        ];
        if (conflicts.Count > 0)
        {
            int resolvedCount = conflicts.Count(x => _resolutions.ContainsKey(GetRecordName(x)));
            choices.Add((ESyncChoice.ResolveConflicts, "Resolve conflicts",
                $"{conflicts.Count} conflicts, {resolvedCount} resolved"));
        }

        if (firstSyncConflictsCount > 0)
        {
            choices.Add((ESyncChoice.LocalWinsFirstSyncConflicts, "Local wins for first sync conflicts",
                $"{firstSyncConflictsCount} conflicts"));
        }

        choices.Add((ESyncChoice.ShowDetails, "Show details", "the records to sync"));
        choices.Add((ESyncChoice.DryRun, "Dry run", "what Apply sends, nothing is sent"));
        choices.Add((ESyncChoice.ExcludeRecord, "Exclude record from sync", "on this computer"));
        choices.Add((ESyncChoice.Cancel, "Cancel", "nothing changes"));
        return choices;
    }

    private string DescribeOperations(RegistrySyncPlan plan, bool includePulls, bool includePushes)
    {
        RegistrySyncOperations operations =
            RegistrySyncEngine.GetOperations(plan, CreateSelection(plan, includePulls, includePushes));
        return $"send {operations.ServerItems.Count}, change here {operations.LocalItems.Count}";
    }

    //Pull only სერვერის სასარგებლოდ გადაწყვეტილ კონფლიქტებს ასრულებს, Push only კი ლოკალურის სასარგებლოდ გადაწყვეტილს
    private RegistrySyncSelection CreateSelection(RegistrySyncPlan plan, bool includePulls, bool includePushes)
    {
        Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution> resolutions = [];
        foreach (RegistrySyncPlanItem item in GetConflicts(plan))
        {
            if (_resolutions.TryGetValue(GetRecordName(item), out ERegistryConflictResolution resolution) &&
                (resolution == ERegistryConflictResolution.Local ? includePushes : includePulls))
            {
                resolutions.Add(item, resolution);
            }
        }

        return new RegistrySyncSelection
        {
            IncludePulls = includePulls,
            IncludePushes = includePushes,
            StopOnFailure = true,
            ConflictResolutions = resolutions
        };
    }

    private static List<RegistrySyncPlanItem> GetConflicts(RegistrySyncPlan plan)
    {
        return [.. plan.Items.Where(x => x.Action == ERegistrySyncAction.Conflict)];
    }

    private void ResolveConflicts(RegistrySyncPlan plan)
    {
        List<RegistrySyncPlanItem> conflicts = GetConflicts(plan);
        for (int i = 0; i < conflicts.Count; i++)
        {
            ResolveConflict(conflicts[i], $"Conflict {i + 1}/{conflicts.Count}");
        }
    }

    private void ResolveConflict(RegistrySyncPlanItem item, string title)
    {
        string recordName = GetRecordName(item);
        string current = _resolutions.TryGetValue(recordName, out ERegistryConflictResolution resolution)
            ? $", current choice: {resolution}"
            : string.Empty;
        Console.WriteLine($"{title}: {recordName} - {DescribeConflict(item.Conflict)}{current}");
        while (true)
        {
            var menuSet = new CliMenuSet();
            menuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Local",
                item.Local is null ? "delete it on the server" : "send this computer's version to the server"));
            menuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Server",
                item.Server is null ? "delete it on this computer" : "take the server's version to this computer"));
            menuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Skip", "keep the conflict for a later sync"));
            menuSet.AddMenuItem(
                new MenuCommandWithStatusCliMenuCommand("Show differences", "field by field, secrets are hidden"));
            switch (_inputIdFromMenuList($"resolution of {recordName}", menuSet))
            {
                case LocalResolutionId:
                    _resolutions[recordName] = ERegistryConflictResolution.Local;
                    return;
                case ServerResolutionId:
                    _resolutions[recordName] = ERegistryConflictResolution.Server;
                    return;
                case ShowDifferencesId:
                    WriteDifferences(item);
                    break;
                default:
                    _resolutions.Remove(recordName);
                    return;
            }
        }
    }

    //seed: პირველი სინქრონიზაციისას სერვერზე უკვე არსებული, მაგრამ განსხვავებული ჩანაწერები (მაგ. ძველი
    //ბრძანებებით ატვირთული Gits) ლოკალურით იცვლება
    private void ResolveFirstSyncConflictsAsLocal(RegistrySyncPlan plan)
    {
        List<RegistrySyncPlanItem> conflicts =
            [.. GetConflicts(plan).Where(x => x.Conflict == ERegistrySyncConflict.FirstSyncDiffers)];
        foreach (RegistrySyncPlanItem item in conflicts)
        {
            _resolutions[GetRecordName(item)] = ERegistryConflictResolution.Local;
        }

        Console.WriteLine($"{conflicts.Count} first sync conflicts are resolved with the local records: " +
                          "Apply or Push only sends them");
    }

    private static void WriteDifferences(RegistrySyncPlanItem item)
    {
        Console.WriteLine($"Differences of {GetRecordName(item)} (secrets are hidden):");
        foreach (RegistryFieldDifference difference in RegistryRecordDiff.Compare(item.Local?.Contract,
                     item.Server?.Contract))
        {
            Console.WriteLine($"  {difference.Path}");
            Console.WriteLine($"    local:  {difference.Local ?? "(none)"}");
            Console.WriteLine($"    server: {difference.Server ?? "(none)"}");
        }
    }

    //ჩანაწერები, რომლებიც InSync არ არის, მოქმედებების მიხედვით; განსხვავებული ველების მხოლოდ სახელები ჩანს
    private static void WriteDetails(RegistrySyncPlan plan)
    {
        foreach (IGrouping<ERegistrySyncAction, RegistrySyncPlanItem> items in plan.Items
                     .Where(x => x.Action != ERegistrySyncAction.InSync).GroupBy(x => x.Action).OrderBy(x => x.Key))
        {
            Console.WriteLine($"{items.Key}:");
            foreach (RegistrySyncPlanItem item in items)
            {
                Console.WriteLine($"  {GetRecordName(item)}: {DescribeItem(item)}");
            }
        }
    }

    //ჩანაწერი, რომელიც InSync არ არის: Push სერვერის ოპერაციით, Pull ლოკალურით, კონფლიქტი თავისი სახით, დანარჩენი
    //(Skipped) გამორიცხულია
    private static string DescribeItem(RegistrySyncPlanItem item)
    {
        string action = item.Action switch
        {
            ERegistrySyncAction.Push => GetServerOperationName(item),
            ERegistrySyncAction.Pull => GetLocalOperationName(item),
            ERegistrySyncAction.Conflict => DescribeConflict(item.Conflict),
            _ => "excluded on this computer"
        };
        List<string> fieldNames = item.Local is null || item.Server is null
            ? []
            : RegistryRecordDiff.GetChangedFieldNames(item.Local.Contract, item.Server.Contract);
        return fieldNames.Count == 0 ? action : $"{action} ({string.Join(", ", fieldNames)})";
    }

    //კონფლიქტის ჩანაწერს სახე ყოველთვის აქვს, ამიტომ ბოლო შემთხვევა FirstSyncDiffers-ია
    private static string DescribeConflict(ERegistrySyncConflict conflict)
    {
        return conflict switch
        {
            ERegistrySyncConflict.BothChanged => "changed here and on the server",
            ERegistrySyncConflict.DeletedOnServer => "deleted on the server, changed here",
            ERegistrySyncConflict.DeletedLocally => "deleted here, changed on the server",
            _ => "first sync, differs here and on the server"
        };
    }

    //Dry run: Apply-ის სერვერის ოპერაციები შესრულების რიგით და ლოკალური ცვლილებები. არაფერი იგზავნება და იცვლება
    private void WriteDryRun(RegistrySyncPlan plan)
    {
        RegistrySyncOperations operations = RegistrySyncEngine.GetOperations(plan, CreateSelection(plan, true, true));
        Console.WriteLine($"Dry run: Apply would send {operations.ServerItems.Count} records to SupportToolsServer, " +
                          "nothing is sent now");
        for (int i = 0; i < operations.ServerItems.Count; i++)
        {
            RegistrySyncPlanItem item = operations.ServerItems[i];
            Console.WriteLine($"  {i + 1}. {GetServerOperationName(item)} {GetRecordName(item)}");
        }

        Console.WriteLine($"Apply would change {operations.LocalItems.Count} records on this computer");
        foreach (RegistrySyncPlanItem item in operations.LocalItems)
        {
            Console.WriteLine($"  {GetLocalOperationName(item)} {GetRecordName(item)}");
        }
    }

    //ჩანაწერი, რომლის ლოკალური მხარე არ არის, სერვერიდან იშლება (RegistrySyncOperations)
    private static string GetServerOperationName(RegistrySyncPlanItem item)
    {
        return (item.Local, item.Server) switch
        {
            (null, _) => "delete",
            (_, null) => "add",
            _ => "update"
        };
    }

    //ჩანაწერი, რომლის სერვერის მხარე არ არის, ლოკალურად იშლება (RegistrySyncOperations)
    private static string GetLocalOperationName(RegistrySyncPlanItem item)
    {
        return (item.Server, item.Local) switch
        {
            (null, _) => "delete",
            (_, null) => "add",
            _ => "update"
        };
    }

    //ჩანაწერი ExcludedKeys-ში ემატება და შენახვის შემდეგ გეგმა თავიდან იგება. გამორიცხვა ამ კომპიუტერისაა: ჩანაწერი
    //აღარც იგზავნება და აღარც ჩამოდის
    private async Task<RegistrySyncPlan?> ExcludeRecord(RegistrySyncPlan plan, CancellationToken cancellationToken)
    {
        List<RegistrySyncPlanItem> candidates = [.. plan.Items.Where(IsChange)];
        var menuSet = new CliMenuSet();
        foreach (RegistrySyncPlanItem candidate in candidates)
        {
            menuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(GetRecordName(candidate),
                $"{candidate.Action}: {DescribeItem(candidate)}"));
        }

        menuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Back", "exclude nothing"));
        int id = _inputIdFromMenuList("record to exclude", menuSet);
        if (id < 0 || id >= candidates.Count)
        {
            return plan;
        }

        RegistrySyncPlanItem item = candidates[id];
        _parameters.RegistrySyncState.GetOrAddCollection(item.CollectionName).ExcludedKeys.Add(item.Key);
        if (!await _parametersManager.Save(_parameters,
                $"{GetRecordName(item)} is excluded from the registry sync on this computer", null, cancellationToken))
        {
            return null;
        }

        return await CreatePlan(cancellationToken);
    }

    private async Task<bool> Execute(RegistrySyncPlan plan, bool includePulls, bool includePushes,
        CancellationToken cancellationToken)
    {
        RegistrySyncSelection selection = CreateSelection(plan, includePulls, includePushes);
        RegistrySyncOperations operations = RegistrySyncEngine.GetOperations(plan, selection);
        if (operations.ServerItems.Count > 0)
        {
            Console.WriteLine($"Sending {operations.ServerItems.Count} records to SupportToolsServer...");
        }

        //StopOnFailure-ის გამო წარუმატებელი სერვერის ოპერაცია მაქსიმუმ ერთია: მას შემდეგ სერვერს აღარაფერი ეგზავნება
        RegistrySyncReportItem? failure = null;
        RegistrySyncReport report = await _engine.Execute(plan, selection, progress =>
        {
            WriteProgress(progress);
            if (progress.Item.Outcome == ERegistrySyncOutcome.Failed)
            {
                failure = progress.Item;
            }
        }, cancellationToken);

        //ჩამოტანისას ლოკალურ ფორმაში ვერ გარდაქმნილი გზები
        WriteNewWarnings();
        return WriteReport(report, failure, operations);
    }

    private void WriteProgress(RegistrySyncProgress progress)
    {
        RegistrySyncPlanItem planItem = progress.Item.PlanItem;
        RegistrySyncReportItem item = progress.Item;
        string line =
            $"{progress.Number}/{progress.Count} {GetServerOperationName(planItem)} {GetRecordName(planItem)}";
        switch (item.Outcome)
        {
            case ERegistrySyncOutcome.Done:
                Console.WriteLine($"{line}: done");
                break;
            case ERegistrySyncOutcome.Conflict:
                StShared.WriteWarningLine($"{line}: conflict - {item.Error?.Description}", true);
                break;
            default:
                WriteError($"{line}: {item.Outcome} - {item.Error?.Description}");
                break;
        }
    }

    //შედეგის ანგარიში. წარმატებაა, თუ არცერთი ოპერაცია არ ჩავარდა, ახალი კონფლიქტი არ გაჩნდა და ცვლილება შეინახა.
    //შესრულება მხოლოდ მაშინ ხდება, როცა გეგმაში Pull, Push ან კონფლიქტია, ამიტომ რაოდენობების სია ცარიელი არ არის
    private bool WriteReport(RegistrySyncReport report, RegistrySyncReportItem? failure,
        RegistrySyncOperations operations)
    {
        List<string> counts =
        [
            .. ReportedOutcomes
                .Select(outcome => (Outcome: outcome, Count: report.Items.Count(x => x.Outcome == outcome)))
                .Where(x => x.Count > 0).Select(x => $"{DescribeOutcome(x.Outcome)} {x.Count}")
        ];
        Console.WriteLine($"Sync result: {string.Join(", ", counts)}");
        foreach (RegistrySyncReportItem item in report.Items.Where(x =>
                     x.Outcome is ERegistrySyncOutcome.Conflict or ERegistrySyncOutcome.Failed))
        {
            WriteError($"  {DescribeOutcome(item.Outcome)} {GetRecordName(item.PlanItem)}: {item.Error?.Description}");
        }

        if (report.TransportError is not null)
        {
            WriteError("SupportToolsServer became unreachable, the remaining server operations were not executed: " +
                       report.TransportError.Description);
        }
        else if (failure is not null)
        {
            WriteError($"The sync stopped after the failure of {GetRecordName(failure.PlanItem)}: fix the record or " +
                       "exclude it from the sync, then sync again");
        }

        if (report.Changed && !report.Saved)
        {
            WriteError("The parameters were not saved");
            return false;
        }

        WriteBackupNote(report, operations);
        return report.TransportError is null && failure is null &&
               report.Items.All(x => x.Outcome != ERegistrySyncOutcome.Conflict);
    }

    //Save-მა (A4) შეცვლამდე ფაილის ასლი შეინახა, ამიტომ ჩამოტანის შემდეგ ჩანს, სად არის ძველი ფაილი. ჩამოტანა
    //ლოკალურ მონაცემს ცვლის, შეუნახავ ცვლილებაზე კი WriteReport აქამდე აღარ მოდის, ანუ ფაილი შენახულია
    private void WriteBackupNote(RegistrySyncReport report, RegistrySyncOperations operations)
    {
        string? parametersFileName = _parametersManager.ParametersFileName;
        if (!string.IsNullOrWhiteSpace(parametersFileName) && report.Items.Any(x =>
                x.Outcome == ERegistrySyncOutcome.Done && operations.LocalItems.Contains(x.PlanItem)))
        {
            Console.WriteLine($"The parameters file before the sync is kept as {parametersFileName}.<date>.bak");
        }
    }

    //ანგარიშში None (InSync და Skipped) არ ჩანს, ამიტომ ბოლო შემთხვევა NotSelected-ია
    private static string DescribeOutcome(ERegistrySyncOutcome outcome)
    {
        return outcome switch
        {
            ERegistrySyncOutcome.Done => "done",
            ERegistrySyncOutcome.Conflict => "conflict",
            ERegistrySyncOutcome.Failed => "failed",
            ERegistrySyncOutcome.NotExecuted => "not executed",
            _ => "not selected"
        };
    }

    //შეცდომის ყოველ სტრიქონზე პაუზა არ არის: ბრძანების შემდეგ მენიუ თვითონ ჩერდება (Reload)
    private void WriteError(string message)
    {
        StShared.WriteErrorLine(message, true, _logger, false);
    }

    private static string GetRecordName(RegistrySyncPlanItem item)
    {
        return $"{item.CollectionName}/{item.Key}";
    }
}
