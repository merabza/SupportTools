using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.LibDataInput;
using LibGitWork.Helpers;
using LibGitWork.Models;
using LibGitWork.SyncV3;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibGitWork.ToolActions;

//V2-ის სამუშაო უფრო სწრაფად:
//1. SupportTools-ის სამუშაო ფოლდერის კლონი (WorkFolder\Gits) git-ის ბოლო ფოლდერია და ჩვეულებრივად სინქრონდება (ამის
//   შემდეგ V2-ს მისი გაახლება აღარ სჭირდება), პროექტების ხელახლა სკანირების გარეშე.
//2. remote-ის ვერსიები SupportToolsServer-იდან ერთი მოთხოვნით იკითხება (სერვერი მათ 10 წუთს ინახავს), ფოლდერების
//   ლოკალური შემოწმების პარალელურად.
//3. ფოლდერები ლოკალურად პარალელურად მოწმდება (ერთი git status ყოველ ფოლდერზე).
//4. commit-ის მესიჯები პარალელურ სამუშაომდე იკითხება.
//5. git-ები პარალელურად სინქრონდება (ერთი git-ის ფოლდერები თანმიმდევრობით). ცვლილებების გარეშე, remote-ის ცნობილ
//   ვერსიაზე მდგარი ფოლდერი git სერვერს არ მიმართავს; push-ის შემდეგ SupportToolsServer ამას გებულობს.
//SupportToolsServer-ის გარეშე (ან ვერსიის გარეშე დარჩენილი git) ყოველი git-ის პირველი ფოლდერი fetch-ს აკეთებს, მისი
//შედეგი კი იმავე git-ის დანარჩენ ფოლდერებს ემსახურება
public sealed class SyncMultipleProjectsGitsToolActionV3 : ToolAction
{
    //ერთდროულად მაქსიმუმ ამდენი ფოლდერი მოწმდება და ამდენი git სინქრონდება (მომხმარებლის არჩევანი)
    internal const int MaxParallelism = 8;

    private const string ActionName = "Sync Multiple Projects Gits V3";

    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly Func<string, bool, bool> _inputBool;
    private readonly Func<string, string?, string> _inputText;
    private readonly ILogger? _logger;
    private readonly SupportToolsParameters _parameters;
    private readonly IProgressConsole? _progressConsole;
    private readonly SyncMultipleProjectsGitsParametersV2 _syncParameters;
    private readonly string? _useMessage;

    //httpClientFactory=null: SupportToolsServer არ გამოიყენება. progressConsole=null: დაფის გარეშე, მხოლოდ სტრიქონები
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncMultipleProjectsGitsToolActionV3(ILogger? logger, IHttpClientFactory? httpClientFactory,
        SupportToolsParameters parameters, SyncMultipleProjectsGitsParametersV2 syncParameters, string? useMessage,
        Func<string, string?, string> inputText, Func<string, bool, bool> inputBool,
        IProgressConsole? progressConsole) : base(logger, ActionName, null, null, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parameters = parameters;
        _syncParameters = syncParameters;
        _useMessage = useMessage;
        _inputText = inputText;
        _inputBool = inputBool;
        _progressConsole = progressConsole;
    }

    public static SyncMultipleProjectsGitsToolActionV3 Create(ILogger? logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string? projectGroupName, string? projectName, bool includeRemoteProjects,
        string? useMessage = null)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        var syncParameters = SyncMultipleProjectsGitsParametersV2.Create(parameters, projectGroupName, projectName,
            includeRemoteProjects);
        ILogger? loggerOrNull = parameters.LogGitWork ? logger : null;
        //გადამისამართებულ გამოტანაში (ფაილი, სხვა პროგრამა) დაფა არ იხატება, მხოლოდ შედეგების სტრიქონები იწერება
        IProgressConsole? progressConsole = Console.IsOutputRedirected ? null : new SystemProgressConsole();
        return new SyncMultipleProjectsGitsToolActionV3(loggerOrNull, httpClientFactory, parameters, syncParameters,
            useMessage, (fieldName, defaultValue) => Inputer.InputTextRequired(fieldName, defaultValue),
            (fieldName, defaultValue) => Inputer.InputBool(fieldName, defaultValue), progressConsole);
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        List<GitSyncFolder> folders = CollectFolders();
        if (folders.Count == 0)
        {
            WriteLine("No git folders to sync");
            return true;
        }

        var commands = new GitFolderCommands(_parameters.GitExecutablePath);
        List<string> gitNames = [.. folders.Select(x => x.GitName).Distinct(StringComparer.OrdinalIgnoreCase)];

        SupportToolsServerApiClient? apiClient = CreateApiClient();
        //ყველა პროექტის სინქრონიზაცია სახელების გარეშე ითხოვს ყველა git-ის ვერსიას (სახელების გრძელი სია მისამართს
        //ზედმეტად გაზრდიდა), ჯგუფისა და პროექტისა კი მხოლოდ თავისი git-ებისას, რომ სერვერმა ზედმეტი remote-ები არ შეამოწმოს
        Task<Result<List<StsGitRemoteHeadDataModel>>>? headsTask = apiClient?.GetGitRemoteHeads(
            _syncParameters.ProjectGroupName is null && _syncParameters.ProjectName is null ? null : gitNames,
            cancellationToken);

        List<GitFolderInspection> inspections = InspectFolders(commands, folders, cancellationToken);
        Dictionary<string, GitHead> heads = await ReadHeads(headsTask, gitNames);
        Dictionary<string, string> commitMessages = AskCommitMessages(inspections);
        List<GitFolderSyncOutcome> outcomes =
            await SyncGits(commands, apiClient, inspections, heads, commitMessages, cancellationToken);

        WriteSummary(gitNames.Count, outcomes);
        return outcomes.TrueForAll(x => x.Error is null);
    }

    //V2-ის წესით: პროექტები სახელით, თითოეულში მისი git-ები. ფოლდერი ერთხელ ითვლება. ბოლოს ყოველი git-ის სამუშაო
    //ფოლდერის კლონი, რომ მას git-ის push-ები უკვე დახვდეს
    private List<GitSyncFolder> CollectFolders()
    {
        List<GitSyncFolder> folders = [];
        var folderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gits = new Dictionary<string, GitData>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<KeyValuePair<string, ProjectModel>> projects = GitProjectListHelper.CreateProjectsList(
            _syncParameters.Projects, _syncParameters.ProjectGroupName, _syncParameters.ProjectName,
            _syncParameters.IncludeRemoteProjects);
        foreach ((string projectName, ProjectModel project) in projects.OrderBy(x => x.Key))
        {
            foreach (string gitName in project.GitProjectNames)
            {
                var gitSyncParameters = GitSyncParameters.Create(_logger, _parameters, projectName, gitName, true);
                if (gitSyncParameters is null)
                {
                    continue;
                }

                string folderPath = Path.Combine(gitSyncParameters.GitsFolder,
                    gitSyncParameters.GitData.GitProjectFolderName);
                if (folderPaths.Add(folderPath))
                {
                    folders.Add(new GitSyncFolder(gitName, gitSyncParameters.GitData.GitProjectAddress, projectName,
                        folderPath));
                }

                gits.TryAdd(gitName, gitSyncParameters.GitData);
            }
        }

        AddWorkFolderClones(folders, gits);
        return folders;
    }

    //V2-ის GitProjectsUpdater-ის ფოლდერები. სამუშაო ფოლდერის გარეშე V2 ვერ მუშაობს, V3 კი მხოლოდ ამ კლონებს ტოვებს
    private void AddWorkFolderClones(List<GitSyncFolder> folders, Dictionary<string, GitData> gits)
    {
        string? workFolder = _parameters.WorkFolder;
        if (gits.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(workFolder))
        {
            StShared.WriteWarningLine("WorkFolder is not specified: the work folder clones are not synced", true,
                _logger);
            return;
        }

        string gitsFolder = Path.Combine(workFolder, "Gits");
        foreach ((string gitName, GitData gitData) in gits)
        {
            string? folderPath = GitFolderCountHelper.GetProjectFolderName(_logger, workFolder, gitsFolder, gitData);
            if (folderPath is not null)
            {
                folders.Add(new GitSyncFolder(gitName, gitData.GitProjectAddress, null, folderPath));
            }
        }
    }

    private SupportToolsServerApiClient? CreateApiClient()
    {
        if (_httpClientFactory is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_parameters.SupportToolsServerWebApiClientName))
        {
            StShared.WriteWarningLine(
                "SupportToolsServerWebApiClientName is not specified: every git is checked with fetch", true, _logger);
            return null;
        }

        try
        {
            return _parameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
        }
        catch (InvalidOperationException e)
        {
            StShared.WriteWarningLine($"{e.Message}: every git is checked with fetch", true, _logger);
            return null;
        }
    }

    private List<GitFolderInspection> InspectFolders(GitFolderCommands commands, List<GitSyncFolder> folders,
        CancellationToken cancellationToken)
    {
        var inspections = new GitFolderInspection[folders.Count];
        int doneCount = 0;
        using (var board = new ParallelProgressBoard(_progressConsole))
        {
            board.SetHeader(InspectionHeader(folders.Count, 0));
            Parallel.For(0, folders.Count,
                new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism, CancellationToken = cancellationToken },
                i =>
                {
                    GitSyncFolder folder = folders[i];
                    GitFolderInspection inspection;
                    using (board.StartLine($"{folder.GitName}  check  {folder.FolderPath}"))
                    {
                        inspection = GitFolderInspection.Inspect(commands, folder);
                    }

                    inspections[i] = inspection;
                    if (inspection.Kind == EGitFolderInspectionKind.Failed)
                    {
                        Report(board, $"{folder.GitName}  {folder.FolderPath}: {inspection.ErrorMessage}", true);
                    }
                    else if (inspection.HasChanges)
                    {
                        Report(board, $"{folder.GitName}  {folder.FolderPath}: has changes", false);
                    }

                    board.SetHeader(InspectionHeader(folders.Count, Interlocked.Increment(ref doneCount)));
                });
        }

        WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Checked {inspections.Length} folders: {inspections.Count(x => x.HasChanges)} with changes, {inspections.Count(x => x.Kind == EGitFolderInspectionKind.NeedsClone)} to clone, {inspections.Count(x => x.Kind == EGitFolderInspectionKind.Failed)} failed"));
        return [.. inspections];
    }

    private static string InspectionHeader(int count, int doneCount)
    {
        return string.Create(CultureInfo.InvariantCulture, $"Checking {count} folders: {doneCount} done");
    }

    private async Task<Dictionary<string, GitHead>> ReadHeads(Task<Result<List<StsGitRemoteHeadDataModel>>>? headsTask,
        List<string> gitNames)
    {
        var heads = new Dictionary<string, GitHead>(StringComparer.OrdinalIgnoreCase);
        if (headsTask is null)
        {
            return heads;
        }

        //სერვერი ვადაგასულ ვერსიებს git ls-remote-ით ამოწმებს, რასაც რამდენიმე წამი შეიძლება დასჭირდეს
        if (!headsTask.IsCompleted)
        {
            WriteLine("Waiting for the remote heads from SupportToolsServer...");
        }

        Result<List<StsGitRemoteHeadDataModel>> headsResult = await headsTask;
        if (headsResult.IsFailure)
        {
            StShared.WriteWarningLine(
                $"SupportToolsServer did not return the remote heads ({headsResult.Error.Description}): every git is checked with fetch",
                true, _logger);
            return heads;
        }

        foreach (StsGitRemoteHeadDataModel head in headsResult.Value)
        {
            if (!string.IsNullOrWhiteSpace(head.BranchName) && !string.IsNullOrWhiteSpace(head.CommitId))
            {
                heads[head.GitName] = new GitHead(head.BranchName, head.CommitId);
            }
        }

        List<string> gitsWithoutHead = [.. gitNames.Where(x => !heads.ContainsKey(x))];
        WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Remote heads from SupportToolsServer: {gitNames.Count - gitsWithoutHead.Count} of {gitNames.Count} gits"));
        if (gitsWithoutHead.Count > 0)
        {
            StShared.WriteWarningLine($"Checked with fetch: {string.Join(", ", gitsWithoutHead)}", true, _logger);
        }

        return heads;
    }

    //V2-ის წესით: მესიჯი ერთხელ იკითხება, თუ მომხმარებელი მას შემდეგი ცვლილებებისთვისაც დატოვებს; სხვა შემთხვევაში
    //ყოველი ცვლილებიანი ფოლდერისთვის ცალკე. ყველა კითხვა პარალელურ სინქრონიზაციამდე ისმება
    private Dictionary<string, string> AskCommitMessages(List<GitFolderInspection> inspections)
    {
        var commitMessages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<GitSyncFolder> changedFolders = [.. inspections.Where(x => x.HasChanges).Select(x => x.Folder)];
        string? commitMessage = _useMessage;
        bool useSameMessage = _useMessage is not null;
        for (int i = 0; i < changedFolders.Count; i++)
        {
            GitSyncFolder folder = changedFolders[i];
            if (!useSameMessage || commitMessage is null)
            {
                Console.WriteLine($"Commit message for {folder.GitName}  {folder.FolderPath}");
                commitMessage = _inputText("Message",
                    commitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture));
                useSameMessage = i < changedFolders.Count - 1 && _inputBool("Use this message for next commits?", true);
            }

            commitMessages[folder.FolderPath] = commitMessage;
        }

        return commitMessages;
    }

    private async Task<List<GitFolderSyncOutcome>> SyncGits(GitFolderCommands commands,
        SupportToolsServerApiClient? apiClient, List<GitFolderInspection> inspections,
        Dictionary<string, GitHead> heads, Dictionary<string, string> commitMessages,
        CancellationToken cancellationToken)
    {
        List<IGrouping<string, GitFolderInspection>> gits = GroupByGit(inspections);
        var outcomes = new IReadOnlyList<GitFolderSyncOutcome>[gits.Count];
        int doneCount = 0;
        using (var board = new ParallelProgressBoard(_progressConsole))
        {
            board.SetHeader(SyncHeader(gits.Count, 0));
            await Parallel.ForAsync(0, gits.Count,
                new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism, CancellationToken = cancellationToken },
                async (i, token) =>
                {
                    IGrouping<string, GitFolderInspection> git = gits[i];
                    using ProgressBoardLine line = board.StartLine($"{git.Key}  start");
                    var syncer = new GitRepositorySyncer(commands, git, heads.GetValueOrDefault(git.Key),
                        commitMessages, (folder, step) => line.SetText($"{git.Key}  {step}  {folder.FolderPath}"));
                    syncer.Run();
                    outcomes[i] = syncer.Outcomes;
                    ReportGit(board, git.Key, syncer.Outcomes);
                    if (syncer.Pushed && apiClient is not null)
                    {
                        line.SetText($"{git.Key}  notify SupportToolsServer");
                        Result notifyResult = await apiClient.NotifyGitPushed(git.Key, token);
                        if (notifyResult.IsFailure)
                        {
                            Report(board,
                                $"{git.Key}: SupportToolsServer did not take the push notification: {notifyResult.Error.Description}",
                                true);
                        }
                    }

                    board.SetHeader(SyncHeader(gits.Count, Interlocked.Increment(ref doneCount)));
                });
        }

        return [.. outcomes.SelectMany(x => x)];
    }

    //თითო ჯგუფი თითო git-ისთვის, დაწყების რიგით: ცვლილებიანი git-ები პირველები (როგორც V2-ში), დანარჩენები სახელით
    internal static List<IGrouping<string, GitFolderInspection>> GroupByGit(
        IEnumerable<GitFolderInspection> inspections)
    {
        return
        [
            .. inspections.GroupBy(x => x.Folder.GitName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Any(y => y.HasChanges) ? 0 : 1).ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        ];
    }

    private static string SyncHeader(int count, int doneCount)
    {
        return string.Create(CultureInfo.InvariantCulture, $"Syncing {count} gits: {doneCount} done");
    }

    //სტრიქონი იწერება მხოლოდ იმ ფოლდერისთვის, სადაც რამე შეიცვალა ან შეცდომა მოხდა. დანარჩენებს ჯამი ითვლის
    private void ReportGit(ParallelProgressBoard board, string gitName, IReadOnlyList<GitFolderSyncOutcome> outcomes)
    {
        foreach (GitFolderSyncOutcome outcome in outcomes)
        {
            //ლოკალური შემოწმების შეცდომა უკვე დაიწერა
            if (outcome.Inspection.Kind == EGitFolderInspectionKind.Failed)
            {
                continue;
            }

            if (outcome.Error is not null)
            {
                Report(board, $"{gitName}  {outcome.Folder.FolderPath}: {outcome.Error}", true);
                continue;
            }

            List<string> actions = [];
            AddIf(actions, outcome.Cloned, "cloned");
            AddIf(actions, outcome.Committed, "committed");
            AddIf(actions, outcome.Pulled, "pulled");
            AddIf(actions, outcome.Pushed,
                outcome.PublishedBranch is null ? "pushed" : $"published branch {outcome.PublishedBranch}");
            if (actions.Count > 0)
            {
                Report(board, $"{gitName}  {outcome.Folder.FolderPath}: {string.Join(", ", actions)}", false);
            }
        }
    }

    private static void AddIf(List<string> actions, bool condition, string action)
    {
        if (condition)
        {
            actions.Add(action);
        }
    }

    private void WriteSummary(int gitCount, List<GitFolderSyncOutcome> outcomes)
    {
        int failedCount = outcomes.Count(x => x.Error is not null);
        string summary = string.Create(CultureInfo.InvariantCulture,
            $"Synced {gitCount} gits, {outcomes.Count} folders: {outcomes.Count(x => x.WithoutRequest)} without a request to the git server, {outcomes.Count(x => x.Fetched)} fetched, {outcomes.Count(x => x.Cloned)} cloned, {outcomes.Count(x => x.Committed)} committed, {outcomes.Count(x => x.Pulled)} pulled, {outcomes.Count(x => x.Pushed)} pushed, {failedCount} failed");
        if (failedCount > 0)
        {
            StShared.WriteWarningLine(summary, true, _logger);
            return;
        }

        WriteLine(summary);
    }

    private void Report(ParallelProgressBoard board, string text, bool isError)
    {
        board.WriteLine(text, isError ? ConsoleColor.Red : null);
        if (isError)
        {
            _logger?.LogError("{Text}", text);
        }
        else
        {
            LogInformation(text);
        }
    }

    private void WriteLine(string text)
    {
        Console.WriteLine(text);
        LogInformation(text);
    }

    private void LogInformation(string text)
    {
        if (_logger is not null && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("{Text}", text);
        }
    }
}
