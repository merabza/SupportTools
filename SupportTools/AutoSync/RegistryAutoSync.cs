using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.Menu.SyncRegistry;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.AutoSync;

//რეესტრის ავტომატური სინქრონიზაცია SupportToolsServer-თან (D2, README §4.3, G5). opt-in-ია: ალამი
//AutoSyncWithSupportToolsServer ამ კომპიუტერისაა. ხელით ბრძანება (SyncRegistryCliMenuCommand) კონფლიქტებისა და რთული
//შემთხვევებისთვის რჩება. ორი მომენტი:
//- PullOnStart: პარამეტრების ჩატვირთვის შემდეგ, მენიუმდე და --run-მდე. სრულდება მხოლოდ არაკონფლიქტური Pull-ები,
//  წაშლების გარდა (მომხმარებლის გადაწყვეტილება: ცარიელმა ან სხვა სერვერმა ლოკალური რეესტრი ჩუმად არ უნდა წაშალოს).
//  ჩანს ერთხაზიანი შეჯამება (მენიუს რეჟიმში მთავარი მენიუს სათაურის ქვეშ). გასაგზავნი, სერვერზე წაშლილი და
//  კონფლიქტური ჩანაწერებისას იმავე სტრიქონში ჩანს რჩევა, გამოიძახო ხელით სინქრონიზაცია;
//- PushAfterSave: წარმატებული ლოკალური შენახვის შემდეგ (AutoSyncParametersManager) სრულდება მხოლოდ არაკონფლიქტური
//  Push-ები. კონფლიქტი გაფრთხილებაა: ლოკალური ცვლილება მომლოდინე რჩება და შემდეგ სინქრონიზაციაზე ჩანს. სერვერს არაფერი
//  ეკითხება, თუ ბოლო სინქრონიზაციის შემდეგ ლოკალური ცვლილება არ არის (RegistrySyncEngine.GetLocalChanges) ან ზუსტად ეს
//  ცვლილებები უკვე სცადა: ერთი ოპერაცია ფაილს 2–3-ჯერ ინახავს (ParCruder + Cruder) და მხოლოდ პირველი შენახვა აგზავნის.
//დაცვები:
//- მდგომარეობა სხვა სერვერისაა (RegistrySyncStateModel.ServerAddress, მომხმარებლის გადაწყვეტილება): გაფრთხილება და
//  არაფერი. ხელით სინქრონიზაცია ახალ მისამართს ჩაწერს; ცარიელი მისამართი (ძველი ფაილი) მიიღება და ჩაიწერება;
//- სერვერი მიუწვდომელია (ქსელის შეცდომა ან ერთ მოთხოვნაზე SupportToolsServerSyncTimeoutSeconds-ზე მეტი ლოდინი):
//  გაფრთხილება და ავტოსინქრონიზაცია პროგრამის დახურვამდე ჩერდება (მომხმარებლის გადაწყვეტილება). პროგრამა ლოკალური
//  ასლით მუშაობს და ყოველი შენახვა სერვერს აღარ ელოდება;
//- ავტოსინქრონიზაციის შეცდომა პროგრამას არ აჩერებს და ლოკალურ შენახვას არ აფუჭებს.
//ძრავა მდგომარეობას შიდა მენეჯერით (SupportToolsMainParametersManager) ინახავს და არა decorator-ით, ამიტომ ძრავის Save
//ახალ Push-ს არ იწვევს. საიდუმლოებები არსად იბეჭდება: ჩანაწერები სახელით ჩანს, API კლიენტი კონსოლის გარეშე იქმნება
public sealed class RegistryAutoSync
{
    private const string Prefix = "Registry auto-sync: ";

    //Push-ის შემდეგ რამდენი ჩანაწერის სახელი ჩანს ერთ სტრიქონში; მეტისას მხოლოდ რაოდენობა
    private const int MaxShownNamesCount = 5;

    private const string ManualSyncAdvice = $"run \"{SyncRegistryCliMenuCommand.CommandName}\"";

    //გაშვებისას: არაკონფლიქტური Pull-ები წაშლების გარეშე
    private static readonly RegistrySyncSelection StartupSelection =
        new() { IncludePulls = true, IncludePullDeletes = false };

    private readonly Func<SupportToolsServerApiClient, SupportToolsParameters, PathMapper, RegistrySyncWarnings,
        IEnumerable<IRegistrySyncAdapter>> _createAdapters;

    //გაშვების გამონატანი, რომელიც მენიუს სათაურის ქვეშ უნდა გამოჩნდეს (PullOnStart-ის deferOutput)
    private readonly List<(string Message, bool IsWarning)> _deferredOutput = [];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    //გამონატანი ჯერ არ იწერება, ინახება _deferredOutput-ში
    private bool _defersOutput;

    //სერვერი მიუწვდომელი აღმოჩნდა: ავტოსინქრონიზაცია პროგრამის დახურვამდე აღარ სრულდება
    private bool _isSuspended;

    //ბოლო Push-ის ცდის ლოკალური ცვლილებები (GetLocalChanges-ის ანაბეჭდი): იგივე ცვლილებები აღარ იგზავნება
    private string? _lastPushedChanges;

    //ბოლოს ნაჩვენები პრობლემა (პარამეტრები, სხვა სერვერი, ლოკალური ჩანაწერები), რომ ყოველ შენახვაზე არ განმეორდეს
    private string? _lastShownProblem;

    public RegistryAutoSync(ILogger<RegistryAutoSync> logger, IHttpClientFactory httpClientFactory,
        SupportToolsMainParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        RegistrySyncAdapterFactory.CreateAdapters)
    {
    }

    //parametersManager შიდა მენეჯერია (არა decorator). ადაპტერების აწყობა პარამეტრადაა გამოტანილი, რომ ტესტებმა fake
    //ადაპტერები მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal RegistryAutoSync(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager,
        Func<SupportToolsServerApiClient, SupportToolsParameters, PathMapper, RegistrySyncWarnings,
            IEnumerable<IRegistrySyncAdapter>> createAdapters)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _createAdapters = createAdapters;
    }

    //deferOutput: მენიუ გაშვებისას ეკრანს ასუფთავებს (CliAppLoop.Run), ამიტომ მენიუს რეჟიმში შეჯამება ინახება და მას
    //მთავარი მენიუს პირველი აგება წერს სათაურის ქვეშ (WriteDeferredOutput). --run-ის რეჟიმში ის მაშინვე იწერება
    public async Task PullOnStart(bool deferOutput, CancellationToken cancellationToken = default)
    {
        if (!IsOn(out SupportToolsParameters? parameters))
        {
            return;
        }

        _defersOutput = deferOutput;
        try
        {
            await Pull(parameters, cancellationToken);
        }
        catch (Exception e)
        {
            WriteWarning($"the registry was not taken from SupportToolsServer: {e.Message}");
        }
        finally
        {
            _defersOutput = false;
        }
    }

    //გაშვების შენახული გამონატანი. ის ერთხელ იწერება: შემდეგი გამოძახება აღარაფერს წერს
    public void WriteDeferredOutput()
    {
        foreach ((string message, bool isWarning) in _deferredOutput)
        {
            Write(message, isWarning);
        }

        _deferredOutput.Clear();
    }

    //გამოიძახება მხოლოდ წარმატებული ლოკალური შენახვის შემდეგ და არასოდეს ისვრის
    public async Task PushAfterSave(CancellationToken cancellationToken = default)
    {
        if (_isSuspended || !IsOn(out SupportToolsParameters? parameters))
        {
            return;
        }

        try
        {
            await Push(parameters, cancellationToken);
        }
        catch (Exception e)
        {
            WriteWarning($"the changes were not sent to SupportToolsServer: {e.Message}");
        }
    }

    private bool IsOn([NotNullWhen(true)] out SupportToolsParameters? parameters)
    {
        parameters = _parametersManager.Parameters as SupportToolsParameters;
        return parameters is { AutoSyncWithSupportToolsServer: true };
    }

    private async Task Pull(SupportToolsParameters parameters, CancellationToken cancellationToken)
    {
        RegistrySyncEngine? engine = CreateEngine(parameters, out RegistrySyncWarnings warnings,
            out PathMapper pathMapper);
        if (engine is null)
        {
            return;
        }

        RegistrySyncPlan? plan = await CreatePlan(engine, cancellationToken);
        if (plan is null)
        {
            return;
        }

        RegistrySyncReport report = await engine.Execute(plan, StartupSelection, null, cancellationToken);
        WritePullSummary(plan, report, warnings.Items.Count + pathMapper.Issues.Count);
        WriteExecutionProblems(report);
    }

    private async Task Push(SupportToolsParameters parameters, CancellationToken cancellationToken)
    {
        RegistrySyncEngine? engine = CreateEngine(parameters, out _, out _);
        if (engine is null)
        {
            return;
        }

        Result<List<RegistrySyncLocalChange>> changes = engine.GetLocalChanges();
        if (changes.IsFailure)
        {
            WriteProblem(changes.Error.Description);
            return;
        }

        _lastShownProblem = null;
        if (changes.Value.Count == 0)
        {
            _lastPushedChanges = null;
            return;
        }

        string pushedChanges = string.Join('\n', changes.Value.Select(x => $"{x.CollectionName}/{x.Key}/{x.Hash}"));
        if (pushedChanges == _lastPushedChanges)
        {
            return;
        }

        _lastPushedChanges = pushedChanges;
        RegistrySyncPlan? plan = await CreatePlan(engine, cancellationToken);
        if (plan is null)
        {
            return;
        }

        RegistrySyncReport report = await engine.Execute(plan, RegistrySyncSelection.PushOnly, null, cancellationToken);
        WritePushReport(report);
        WriteExecutionProblems(report);
    }

    //SupportToolsServerWebApiClientName-ის ApiClient-ით (bootstrap, G6) აწყობილი ძრავა. null — ავტოსინქრონიზაცია ვერ
    //დაიწყება და მიზეზი უკვე ჩანს
    private RegistrySyncEngine? CreateEngine(SupportToolsParameters parameters, out RegistrySyncWarnings warnings,
        out PathMapper pathMapper)
    {
        warnings = new RegistrySyncWarnings();
        pathMapper = new PathMapper(parameters.PathMappings);
        string? apiClientName = parameters.SupportToolsServerWebApiClientName;
        if (string.IsNullOrWhiteSpace(apiClientName))
        {
            WriteProblem("SupportToolsServerWebApiClientName is not set: choose the ApiClient of SupportToolsServer " +
                         "in Support Tools Parameters Editor");
            return null;
        }

        ApiClientSettings? apiClientSettings = parameters.ApiClients.GetValueOrDefault(apiClientName);
        if (apiClientSettings is null)
        {
            WriteProblem($"ApiClient {apiClientName} (SupportToolsServerWebApiClientName) does not exist");
            return null;
        }

        string? server = apiClientSettings.Server;
        if (string.IsNullOrWhiteSpace(server))
        {
            WriteProblem($"the server address of ApiClient {apiClientName} is not set");
            return null;
        }

        string? stateServer = parameters.RegistrySyncState.ServerAddress;
        if (!string.IsNullOrWhiteSpace(stateServer) && !IsSameAddress(stateServer, server))
        {
            WriteProblem($"the registry sync state belongs to SupportToolsServer {stateServer}, ApiClient " +
                         $"{apiClientName} points to {server}: {ManualSyncAdvice} to sync with the new server");
            return null;
        }

        var apiClient = new SupportToolsServerApiClient(_logger,
            new TimeoutHttpClientFactory(_httpClientFactory, GetTimeout(parameters)), server, apiClientSettings.ApiKey,
            false);
        return new RegistrySyncEngine(_createAdapters(apiClient, parameters, pathMapper, warnings), _parametersManager)
        {
            ServerAddress = server
        };
    }

    //მისამართები ერთი სერვერისაა, თუ მხოლოდ რეგისტრით, ბოლო „/“-ით ან კიდეებზე ჰარებით განსხვავდება
    private static bool IsSameAddress(string address1, string address2)
    {
        return string.Equals(address1.Trim().TrimEnd('/'), address2.Trim().TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan GetTimeout(SupportToolsParameters parameters)
    {
        int seconds = parameters.SupportToolsServerSyncTimeoutSeconds;
        return TimeSpan.FromSeconds(seconds > 0
            ? seconds
            : SupportToolsParameters.DefaultSupportToolsServerSyncTimeoutSeconds);
    }

    //გეგმა. სერვერი მიუწვდომელია → ავტოსინქრონიზაცია ჩერდება; სხვა შეცდომა (მაგ. 401, ლოკალური გასაღებები, რომლებიც
    //მხოლოდ რეგისტრით განსხვავდება) მხოლოდ გაფრთხილებაა
    private async Task<RegistrySyncPlan?> CreatePlan(RegistrySyncEngine engine, CancellationToken cancellationToken)
    {
        Result<RegistrySyncPlan> plan = await engine.CreatePlan(cancellationToken);
        if (plan.IsSuccess)
        {
            return plan.Value;
        }

        if (plan.Error.Code == RegistrySyncServerErrorCodes.RequestFailed)
        {
            Suspend(plan.Error);
        }
        else
        {
            WriteWarning($"the sync plan was not created: {plan.Error.Description}");
        }

        return null;
    }

    //ერთი სტრიქონი: რა ჩამოვიდა და რა ელოდება ხელით სინქრონიზაციას
    private void WritePullSummary(RegistrySyncPlan plan, RegistrySyncReport report, int warningsCount)
    {
        int takenCount = report.Items.Count(x =>
            x.PlanItem.Action == ERegistrySyncAction.Pull && x.Outcome == ERegistrySyncOutcome.Done);
        List<string> waiting =
        [
            .. new (int Count, string Name)[]
            {
                (plan.Items.Count(x => x.Action == ERegistrySyncAction.Push), "to send"),
                (plan.Items.Count(x => x.Action == ERegistrySyncAction.Pull && x.Server is null),
                    "deleted on the server"),
                (plan.Items.Count(x => x.Action == ERegistrySyncAction.Conflict), "in conflict"),
                (report.Items.Count(x => x.PlanItem.Action == ERegistrySyncAction.Pull && x.Outcome is ERegistrySyncOutcome.Failed or ERegistrySyncOutcome.NotExecuted),
                    "not taken"),
                (warningsCount, warningsCount == 1 ? "warning" : "warnings")
            }.Where(x => x.Count > 0).Select(x => $"{x.Count} {x.Name}")
        ];
        string taken = takenCount switch
        {
            0 => "nothing new on SupportToolsServer",
            1 => "1 record taken from SupportToolsServer",
            _ => $"{takenCount} records taken from SupportToolsServer"
        };
        if (waiting.Count == 0)
        {
            Write(taken, false);
            return;
        }

        WriteWarning($"{taken}; {ManualSyncAdvice} for {string.Join(", ", waiting)}");
    }

    //გაგზავნილი ჩანაწერები, მომლოდინე კონფლიქტები და სერვერის უარყოფილი ჩანაწერები
    private void WritePushReport(RegistrySyncReport report)
    {
        List<string> sent =
        [
            .. report.Items.Where(x =>
                    x.PlanItem.Action == ERegistrySyncAction.Push && x.Outcome == ERegistrySyncOutcome.Done)
                .Select(x => GetRecordName(x.PlanItem))
        ];
        if (sent.Count > 0)
        {
            Write($"sent to SupportToolsServer: {JoinNames(sent)}", false);
        }

        //გეგმის კონფლიქტი, ან სერვერის 409/404: ჩანაწერი შუალედში შეიცვალა ან წაიშალა
        List<string> conflicts =
        [
            .. report.Items.Where(x => x.Outcome == ERegistrySyncOutcome.Conflict || x is
            {
                Outcome: ERegistrySyncOutcome.NotSelected,
                PlanItem.Action: ERegistrySyncAction.Conflict
            }).Select(x =>
                x.Outcome == ERegistrySyncOutcome.Conflict
                    ? $"{GetRecordName(x.PlanItem)} (changed on the server meanwhile)"
                    : $"{GetRecordName(x.PlanItem)} ({RegistrySyncSession.DescribeConflict(x.PlanItem)})")
        ];
        if (conflicts.Count > 0)
        {
            WriteWarning($"the local changes of {JoinNames(conflicts)} wait: {ManualSyncAdvice}");
        }

        foreach (RegistrySyncReportItem item in report.Items.Where(x => x.Outcome == ERegistrySyncOutcome.Failed))
        {
            WriteWarning($"{GetRecordName(item.PlanItem)} was not sent: {item.Error?.Description}");
        }
    }

    //რამდენიმე სახელი ჩამოთვლილია, მეტისას მხოლოდ მათი რაოდენობა ჩანს
    private static string JoinNames(List<string> names)
    {
        return names.Count <= MaxShownNamesCount ? string.Join(", ", names) : $"{names.Count} records";
    }

    //შესრულების საერთო პრობლემები: სერვერი შუალედში გაითიშა, ან ცვლილება ვერ შეინახა
    private void WriteExecutionProblems(RegistrySyncReport report)
    {
        if (report.TransportError is not null)
        {
            Suspend(report.TransportError);
        }

        if (report.Changed && !report.Saved)
        {
            WriteWarning("the parameters were not saved after the sync");
        }
    }

    private void Suspend(Error error)
    {
        _isSuspended = true;
        WriteWarning($"SupportToolsServer is not reachable ({error.Description}), working with the local copy. " +
                     $"Auto-sync is off until SupportTools restarts, {ManualSyncAdvice} when the server is back");
    }

    //პრობლემა, რომელიც ავტოსინქრონიზაციას არ უშვებს. ზედიზედ ერთი და იგივე პრობლემა ერთხელ ჩანს
    private void WriteProblem(string problem)
    {
        if (problem == _lastShownProblem)
        {
            return;
        }

        _lastShownProblem = problem;
        WriteWarning(problem);
    }

    private void WriteWarning(string message)
    {
        Write(message, true);
    }

    private void Write(string message, bool isWarning)
    {
        if (_defersOutput)
        {
            _deferredOutput.Add((message, isWarning));
        }
        else if (isWarning)
        {
            StShared.WriteWarningLine($"{Prefix}{message}", true, _logger);
        }
        else
        {
            Console.WriteLine($"{Prefix}{message}");
        }
    }

    private static string GetRecordName(RegistrySyncPlanItem item)
    {
        return $"{item.CollectionName}/{item.Key}";
    }
}
