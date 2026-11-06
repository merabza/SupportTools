using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.OutdatedPackages;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace LibGitWork.ToolActions;

public sealed class UpdateOutdatedPackagesToolAction : ToolAction
{
    //false: მხოლოდ ამ კომპიუტერზე არსებული პროექტები მუშავდება და დანარჩენები არ იკლონება (D3)
    private readonly bool _includeRemoteProjects;

    //კითხვა (ტექსტი, ნაგულისხმევი პასუხი). ბრძანება კონსოლს აწვდის, ტესტები - თავიანთ პასუხს
    private readonly Func<string, bool, bool> _inputBool;

    //commit-ის შეტყობინების შეყვანა (ველის სახელი, ნაგულისხმევი მნიშვნელობა). ბრძანება კონსოლს აწვდის, ტესტები - თავიანთ პასუხს
    private readonly Func<string, string, string> _inputMessage;
    private readonly ILogger? _logger;
    private readonly IOutdatedPackagesUpdateSteps _steps;
    private readonly TimeProvider _timeProvider;
    private readonly UpdateOutdatedPackagesParameters _updateOutdatedPackagesParameters;

    private UpdateOutdatedPackagesToolAction(ILogger? logger,
        UpdateOutdatedPackagesParameters updateOutdatedPackagesParameters, IOutdatedPackagesUpdateSteps steps,
        bool includeRemoteProjects, bool useConsole, Func<string, string, string> inputMessage,
        Func<string, bool, bool> inputBool, TimeProvider timeProvider) : base(logger, "Update Outdated Packages", null,
        null, useConsole)
    {
        _logger = logger;
        _updateOutdatedPackagesParameters = updateOutdatedPackagesParameters;
        _steps = steps;
        _includeRemoteProjects = includeRemoteProjects;
        _inputMessage = inputMessage;
        _inputBool = inputBool;
        _timeProvider = timeProvider;
    }

    public static UpdateOutdatedPackagesToolAction Create(ILogger logger, IParametersManager parametersManager,
        IOutdatedPackagesUpdateSteps steps, string? projectGroupName, string? projectName, bool includeRemoteProjects,
        bool useConsole, Func<string, string, string> inputMessage, Func<string, bool, bool> inputBool,
        TimeProvider timeProvider)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var updateOutdatedPackagesParameters =
            UpdateOutdatedPackagesParameters.Create(supportToolsParameters, projectGroupName, projectName);
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        return new UpdateOutdatedPackagesToolAction(loggerOrNull, updateOutdatedPackagesParameters, steps,
            includeRemoteProjects, useConsole, inputMessage, inputBool, timeProvider);
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        string useMessage = _inputMessage("Message", $"Update Packages {DateTime.Now.ToString("yyyyMMddHHmm",
            CultureInfo.InvariantCulture)}");

        var checkSyncTime = new PhaseTime();
        bool checkSynced = await Measure(checkSyncTime,
            () => _steps.SyncGits(null, _includeRemoteProjects, $"{useMessage} Check", cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        //ჩავარდნილი ფოლდერები სინქრონიზაციის ანგარიშშია. გაგრძელებისას თითოეულ პროექტს განახლების წინ სინქრონიზაცია
        //თავიდან სცდის (მომხმარებლის არჩევანი)
        if (!checkSynced &&
            !_inputBool("Some gits were not synced (see above). Continue updating packages?", false))
        {
            return false;
        }

        List<List<OutdatedPackagesUpdateStep>> levels = OutdatedPackagesUpdateOrder.Create(
            ProjectPresence.Filter(GetProjectsList(), _includeRemoteProjects), _updateOutdatedPackagesParameters.Gits);

        var syncsBeforeTime = new PhaseTime();
        var packageUpdatesTime = new PhaseTime();
        var syncsAfterTime = new PhaseTime();
        //var packageUpdaters = new Dictionary<string, PackageUpdater>();
        //var updatedGitProjectNames = new List<string>();
        var notSyncedGitNames = new List<string>();
        var failedGitNames = new List<string>();
        var notRestoredGitNames = new List<string>();
        var notPushedGitNames = new List<string>();
        foreach (List<OutdatedPackagesUpdateStep> level in levels)
        {
            List<OutdatedPackagesUpdateStep> preparedSteps = [];
            foreach (OutdatedPackagesUpdateStep step in level)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //დაუსინქრონებელ ფოლდერზე git არ ახლდება, დანარჩენები კი გრძელდება (მომხმარებლის არჩევანი)
                if (await Measure(syncsBeforeTime,
                        () => _steps.SyncGits(step.ProjectName, _includeRemoteProjects, $"{useMessage} Prepare",
                            cancellationToken)))
                {
                    preparedSteps.Add(step);
                }
                else
                {
                    notSyncedGitNames.Add(step.GitName);
                }
            }

            if (preparedSteps.Count == 0)
            {
                continue;
            }

            //დონის git-ები ერთმანეთზე არ არის დამოკიდებული, ამიტომ ერთად, პარალელურად ახლდება; დრო დონის ხანგრძლივობაა.
            //ვერსიები, რომლებითაც restore ჩავარდა, dotnet outdated-ის ჩავარდნის მსგავსად მაინც სინქრონდება
            IReadOnlyList<EPackageUpdateResult> updateResults = await Measure(packageUpdatesTime,
                () => Task.FromResult(_steps.UpdatePackages(preparedSteps, cancellationToken)), preparedSteps.Count);
            for (int i = 0; i < preparedSteps.Count; i++)
            {
                if (updateResults[i] == EPackageUpdateResult.OutdatedFailed)
                {
                    failedGitNames.Add(preparedSteps[i].GitName);
                }
                else if (updateResults[i] == EPackageUpdateResult.RestoreAfterUpdateFailed)
                {
                    notRestoredGitNames.Add(preparedSteps[i].GitName);
                }
            }

            //V3-ის სინქრონიზაციას თავისი დაფა აქვს, ამიტომ ეს სინქები ერთმანეთის მიყოლებით, განახლებების შემდეგ ეშვება.
            //ჩავარდნისას განახლება ლოკალურად რჩება
            foreach (OutdatedPackagesUpdateStep step in preparedSteps)
            {
                if (!await Measure(syncsAfterTime,
                        () => _steps.SyncGits(step.ProjectName, _includeRemoteProjects, $"{useMessage} Update",
                            cancellationToken)))
                {
                    notPushedGitNames.Add(step.GitName);
                }
            }
        }

        //foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        //{
        //    foreach (string gitProjectName in project.GitProjectNames)
        //    {
        //        if (!packageUpdaters.TryGetValue(gitProjectName, out PackageUpdater? value))
        //        {
        //            value = new PackageUpdater(_logger, _parametersManager, gitProjectName, true);
        //            packageUpdaters.Add(gitProjectName, value);
        //        }

        //        value.Add(projectName);
        //    }
        //}

        //foreach (KeyValuePair<string, PackageUpdater> keyValuePair in packageUpdaters.Where(x => x.Value.Count > 0)
        //             .OrderBy(x => x.Key))
        //{
        //    PackageUpdater packageUpdater = keyValuePair.Value;
        //    packageUpdater.Run();
        //}

        WriteTimes(checkSyncTime, syncsBeforeTime, packageUpdatesTime, syncsAfterTime);

        List<string> errorLines = [];
        AddErrorLine(errorLines, "sync before the update failed for", notSyncedGitNames);
        AddErrorLine(errorLines, "dotnet outdated failed for", failedGitNames);
        AddErrorLine(errorLines, "restore after the update failed for", notRestoredGitNames);
        AddErrorLine(errorLines, "sync after the update failed for", notPushedGitNames);
        if (errorLines.Count == 0)
        {
            return true;
        }

        //ჩავარდნილი რეპოების შეჯამება ბოლოს, ერთი პაუზით
        StShared.WriteErrorLine(string.Join(Environment.NewLine, errorLines), true, _logger);
        return false;
    }

    //count: ეტაპის რამდენი გაშვება შესრულდა ამ დროში (დონის პარალელური განახლება ყველა თავის git-ს ითვლის)
    private async Task<T> Measure<T>(PhaseTime phaseTime, Func<Task<T>> action, int count = 1)
    {
        long timestamp = _timeProvider.GetTimestamp();
        T result = await action();
        phaseTime.Add(_timeProvider.GetElapsedTime(timestamp), count);
        return result;
    }

    //ეტაპების ხანგრძლივობა: მისი მიხედვით გადაწყდება, ღირს თუ არა git-ის ან dotnet outdated-ის შემდგომი დაჩქარება
    private void WriteTimes(PhaseTime checkSyncTime, PhaseTime syncsBeforeTime, PhaseTime packageUpdatesTime,
        PhaseTime syncsAfterTime)
    {
        WriteLine("Times of Update Outdated Packages:");
        WriteLine($"  sync of all gits (Check): {FormatTime(checkSyncTime.Elapsed)}");
        WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"  {syncsBeforeTime.Count} syncs before an update (Prepare): {FormatTime(syncsBeforeTime.Elapsed)}"));
        WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"  {packageUpdatesTime.Count} package updates (restore, dotnet outdated, check restore): {FormatTime(packageUpdatesTime.Elapsed)}"));
        WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"  {syncsAfterTime.Count} syncs after an update (Update): {FormatTime(syncsAfterTime.Elapsed)}"));
    }

    private static string FormatTime(TimeSpan time)
    {
        return string.Create(CultureInfo.InvariantCulture,
            $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}");
    }

    private void WriteLine(string text)
    {
        Console.WriteLine(text);
        if (_logger is not null && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("{Text}", text);
        }
    }

    private static void AddErrorLine(List<string> errorLines, string text, List<string> gitNames)
    {
        if (gitNames.Count > 0)
        {
            errorLines.Add($"{text}: {string.Join(", ", gitNames)}");
        }
    }

    private IEnumerable<KeyValuePair<string, ProjectModel>> GetProjectsList()
    {
        if (_updateOutdatedPackagesParameters.ProjectGroupName is null &&
            _updateOutdatedPackagesParameters.ProjectName is null)
        {
            return _updateOutdatedPackagesParameters.Projects;
        }

        if (_updateOutdatedPackagesParameters.ProjectGroupName is not null)
        {
            return _updateOutdatedPackagesParameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _updateOutdatedPackagesParameters.ProjectGroupName);
        }

        return _updateOutdatedPackagesParameters.Projects.Where(x =>
            x.Key == _updateOutdatedPackagesParameters.ProjectName);
    }

    //ეტაპის გაშვებების რაოდენობა და ჯამური ხანგრძლივობა
    private sealed class PhaseTime
    {
        public int Count { get; private set; }
        public TimeSpan Elapsed { get; private set; }

        public void Add(TimeSpan elapsed, int count)
        {
            Count += count;
            Elapsed += elapsed;
        }
    }
}
