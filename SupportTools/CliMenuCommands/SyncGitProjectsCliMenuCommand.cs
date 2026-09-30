using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibMenuInput;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

//კლიენტის (Gits) და სერვერის git ჩანაწერების შედარება და სინქრონიზაცია არჩეული მიმართულებით, ისევე როგორც
//.gitignore ჩანაწერებისთვის. Merge ზედმეტ ჩანაწერებს არ შლის, Sync შლის. სერვერზე git ჩანაწერს არაფერი იყენებს,
//ამიტომ იქ ყოველთვის იშლება. კლიენტზე იშლება მხოლოდ ის, რომელსაც არც ერთი პროექტი არ იყენებს.
//ჩანაწერი მხოლოდ იმ მხარეს ჩაიწერება, სადაც მისი .gitignore შაბლონი არსებობს. შაბლონები აქ არ სინქრონდება
public sealed class SyncGitProjectsCliMenuCommand : CliMenuCommand
{
    private const int MergeUp = 0;
    private const int SyncUp = 1;
    private const int MergeDown = 2;
    private const int SyncDown = 3;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Func<string, CliMenuSet, int> _inputIdFromMenuList;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public SyncGitProjectsCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        (fieldName, listSet) => MenuInputer.InputIdFromMenuList(fieldName, listSet))
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncGitProjectsCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, Func<string, CliMenuSet, int> inputIdFromMenuList) : base(
        "Sync Git Projects With SupportToolsServer...", EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _inputIdFromMenuList = inputIdFromMenuList;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        SupportToolsServerApiClient? supportToolsServerApiClient =
            parameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true, _logger);
            return false;
        }

        Result<List<StsGitDataModel>> serverGitsResult =
            await supportToolsServerApiClient.GetGitRepos(cancellationToken);
        if (serverGitsResult.IsFailure)
        {
            serverGitsResult.Error.PrintErrorsOnConsole();
            return false;
        }

        //სერვერის .gitignore შაბლონები საჭიროა იმის დასადგენად, შეიძლება თუ არა ჩანაწერის სერვერზე ატვირთვა
        Result<List<StsGitIgnoreFileTypeDataModel>> serverPatternsResult =
            await supportToolsServerApiClient.GetGitIgnoreFileTypesList(cancellationToken);
        if (serverPatternsResult.IsFailure)
        {
            serverPatternsResult.Error.PrintErrorsOnConsole();
            return false;
        }

        //ჩანაწერები სახელით ემთხვევა, სერვერის მსგავსად რეგისტრის გაუთვალისწინებლად
        var clientRecords = new Dictionary<string, GitDataModel>(StringComparer.OrdinalIgnoreCase);
        foreach ((string gitName, GitDataModel git) in parameters.Gits)
        {
            clientRecords[gitName] = git;
        }

        Dictionary<string, StsGitDataModel> serverRecords =
            serverGitsResult.Value.ToDictionary(x => x.GitProjectName, StringComparer.OrdinalIgnoreCase);

        List<string> onlyOnClient = [.. clientRecords.Keys.Where(x => !serverRecords.ContainsKey(x)).Order()];
        List<string> onlyOnServer = [.. serverRecords.Keys.Where(x => !clientRecords.ContainsKey(x)).Order()];
        var differences = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach ((string gitName, GitDataModel clientGit) in clientRecords)
        {
            if (serverRecords.TryGetValue(gitName, out StsGitDataModel? serverGit))
            {
                List<string> differentFields = GetDifferentFields(clientGit, serverGit);
                if (differentFields.Count > 0)
                {
                    differences.Add(gitName, differentFields);
                }
            }
        }

        List<string> different = [.. differences.Keys.Order()];

        if (onlyOnClient.Count == 0 && onlyOnServer.Count == 0 && different.Count == 0)
        {
            Console.WriteLine("Client and server git records are identical, nothing to sync");
            return true;
        }

        var serverPatternNames =
            new HashSet<string>(serverPatternsResult.Value.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string?> uploadProblems = onlyOnClient.Concat(different).ToDictionary(x => x,
            x => GetUploadProblem(ToServerModel(x, clientRecords[x]), serverPatternNames));
        Dictionary<string, string?> downloadProblems = onlyOnServer.Concat(different)
            .ToDictionary(x => x, x => GetDownloadProblem(serverRecords[x], parameters.GitIgnorePatterns));
        //კლიენტზე ზედმეტი ჩანაწერის წაშლა შეუძლებელია, თუ მას რომელიმე პროექტი იყენებს
        Dictionary<string, string[]> clientUsages = onlyOnClient.ToDictionary(x => x, x => (string[])
        [
            .. parameters.Projects
                .Where(p => p.Value.GitProjectNames.Concat(p.Value.ScaffoldSeederGitProjectNames)
                    .Contains(x, StringComparer.OrdinalIgnoreCase)).Select(p => p.Key).Order()
        ]);

        Console.WriteLine("Differences between client and server git records:");
        foreach (string name in different)
        {
            WriteRecord(name, $"differs ({string.Join(", ", differences[name])})", uploadProblems[name],
                downloadProblems[name], []);
        }

        foreach (string name in onlyOnClient)
        {
            WriteRecord(name, "only on client", uploadProblems[name], null, clientUsages[name]);
        }

        foreach (string name in onlyOnServer)
        {
            WriteRecord(name, "only on server", null, downloadProblems[name], []);
        }

        Console.WriteLine();

        var upChanges = new Changes([.. onlyOnClient.Where(x => uploadProblems[x] is null)],
            [.. different.Where(x => uploadProblems[x] is null)], onlyOnServer,
            [.. onlyOnClient.Where(x => uploadProblems[x] is not null)],
            [.. different.Where(x => uploadProblems[x] is not null)], []);
        var downChanges = new Changes([.. onlyOnServer.Where(x => downloadProblems[x] is null)],
            [.. different.Where(x => downloadProblems[x] is null)],
            [.. onlyOnClient.Where(x => clientUsages[x].Length == 0)],
            [.. onlyOnServer.Where(x => downloadProblems[x] is not null)],
            [.. different.Where(x => downloadProblems[x] is not null)],
            [.. onlyOnClient.Where(x => clientUsages[x].Length > 0)]);

        var processesMenuSet = new CliMenuSet();
        processesMenuSet.AddMenuItem(
            new MenuCommandWithStatusCliMenuCommand("Merge Up", DescribeChanges("server", upChanges, false)));
        processesMenuSet.AddMenuItem(
            new MenuCommandWithStatusCliMenuCommand("Sync Up", DescribeChanges("server", upChanges, true)));
        processesMenuSet.AddMenuItem(
            new MenuCommandWithStatusCliMenuCommand("Merge Down", DescribeChanges("client", downChanges, false)));
        processesMenuSet.AddMenuItem(
            new MenuCommandWithStatusCliMenuCommand("Sync Down", DescribeChanges("client", downChanges, true)));

        int process = _inputIdFromMenuList("process to run", processesMenuSet);

        switch (process)
        {
            case MergeUp:
            case SyncUp:
                return await Upload(supportToolsServerApiClient, clientRecords, upChanges, process == SyncUp,
                    cancellationToken);
            case MergeDown:
            case SyncDown:
                return await Download(parameters, clientRecords, serverRecords, downChanges, process == SyncDown,
                    cancellationToken);
            default:
                StShared.WriteErrorLine("Selected invalid process", true, _logger);
                return false;
        }
    }

    private async Task<bool> Upload(SupportToolsServerApiClient supportToolsServerApiClient,
        Dictionary<string, GitDataModel> clientRecords, Changes changes, bool withDeletion,
        CancellationToken cancellationToken)
    {
        //ჯერ წაშლა: სერვერზე მისამართი უნიკალურია, ამიტომ კლიენტზე სახელგადარქმეული git-ი
        //მხოლოდ ძველი ჩანაწერის წაშლის შემდეგ აიტვირთება
        List<string> namesToDelete = withDeletion ? changes.ToDelete : [];
        foreach (string name in namesToDelete)
        {
            Result deleteResult = await supportToolsServerApiClient.RemoveGitRepoByKey(name, cancellationToken);
            if (deleteResult.IsFailure)
            {
                deleteResult.Error.PrintErrorsOnConsole();
                return false;
            }
        }

        //ატვირთვა მხოლოდ ამატებს და ანახლებს. .gitignore შაბლონები არ იტვირთება, სერვერზე უკვე უნდა არსებობდეს
        List<StsGitDataModel> gitsToUpload =
        [
            .. changes.ToAdd.Concat(changes.ToUpdate).Select(x => ToServerModel(x, clientRecords[x]))
                .OfType<StsGitDataModel>()
        ];
        if (gitsToUpload.Count > 0)
        {
            Result uploadResult = await supportToolsServerApiClient.UploadGitRepos(
                new SyncGitRequest { Gits = gitsToUpload, GitIgnoreFiles = [] }, cancellationToken);
            if (uploadResult.IsFailure)
            {
                uploadResult.Error.PrintErrorsOnConsole();
                return false;
            }
        }

        WriteNotDone([.. changes.NotToAdd, .. changes.NotToUpdate], "uploaded to server");
        Console.WriteLine(
            $"{gitsToUpload.Count} git records uploaded to server, {namesToDelete.Count} deleted from server");
        return true;
    }

    private async Task<bool> Download(SupportToolsParameters parameters,
        Dictionary<string, GitDataModel> clientRecords, Dictionary<string, StsGitDataModel> serverRecords,
        Changes changes, bool withDeletion, CancellationToken cancellationToken)
    {
        foreach (string name in changes.ToAdd)
        {
            StsGitDataModel serverGit = serverRecords[name];
            parameters.Gits[serverGit.GitProjectName] = new GitDataModel
            {
                GitProjectAddress = serverGit.GitProjectAddress,
                GitProjectFolderName = serverGit.GitProjectFolderName,
                GitIgnorePatternName = GetClientPatternName(serverGit, parameters.GitIgnorePatterns)
            };
        }

        //არსებული ჩანაწერი სახელს ინარჩუნებს, იცვლება მხოლოდ ველები
        foreach (string name in changes.ToUpdate)
        {
            StsGitDataModel serverGit = serverRecords[name];
            GitDataModel clientGit = clientRecords[name];
            clientGit.GitProjectAddress = serverGit.GitProjectAddress;
            clientGit.GitProjectFolderName = serverGit.GitProjectFolderName;
            clientGit.GitIgnorePatternName = GetClientPatternName(serverGit, parameters.GitIgnorePatterns);
        }

        List<string> namesToDelete = withDeletion ? changes.ToDelete : [];
        foreach (string name in namesToDelete)
        {
            parameters.Gits.Remove(name);
        }

        if (!await _parametersManager.Save(parameters, "git records synced from server", null, cancellationToken))
        {
            return false;
        }

        WriteNotDone([.. changes.NotToAdd, .. changes.NotToUpdate], "downloaded from server");
        if (withDeletion && changes.NotToDelete.Count > 0)
        {
            StShared.WriteWarningLine(
                $"Git records used by projects were not deleted from client: {string.Join(", ", changes.NotToDelete)}",
                true, _logger);
        }

        Console.WriteLine(
            $"{changes.ToAdd.Count + changes.ToUpdate.Count} git records downloaded from server, {namesToDelete.Count} deleted from client");
        return true;
    }

    private void WriteNotDone(List<string> names, string action)
    {
        if (names.Count > 0)
        {
            StShared.WriteWarningLine($"Git records were not {action}: {string.Join(", ", names)}", true, _logger);
        }
    }

    private static List<string> GetDifferentFields(GitDataModel clientGit, StsGitDataModel serverGit)
    {
        List<string> differentFields = [];
        if (!string.Equals(clientGit.GitProjectAddress, serverGit.GitProjectAddress, StringComparison.Ordinal))
        {
            differentFields.Add("address");
        }

        if (!string.Equals(clientGit.GitProjectFolderName, serverGit.GitProjectFolderName, StringComparison.Ordinal))
        {
            differentFields.Add("folder name");
        }

        //სერვერი შაბლონს სახელით რეგისტრის გაუთვალისწინებლად პოულობს
        if (!string.Equals(clientGit.GitIgnorePatternName, serverGit.GitIgnorePatternName,
                StringComparison.OrdinalIgnoreCase))
        {
            differentFields.Add(".gitignore pattern");
        }

        return differentFields;
    }

    //სერვერი ბოლომდე შეუვსებელ ჩანაწერს არ იღებს
    private static StsGitDataModel? ToServerModel(string gitName, GitDataModel git)
    {
        return string.IsNullOrWhiteSpace(git.GitProjectAddress) ||
               string.IsNullOrWhiteSpace(git.GitProjectFolderName) ||
               string.IsNullOrWhiteSpace(git.GitIgnorePatternName)
            ? null
            : new StsGitDataModel
            {
                GitProjectName = gitName,
                GitProjectAddress = git.GitProjectAddress,
                GitProjectFolderName = git.GitProjectFolderName,
                GitIgnorePatternName = git.GitIgnorePatternName
            };
    }

    private static string? GetUploadProblem(StsGitDataModel? serverModel, HashSet<string> serverPatternNames)
    {
        if (serverModel is null)
        {
            return "not fully filled";
        }

        return serverPatternNames.Contains(serverModel.GitIgnorePatternName)
            ? null
            : $".gitignore pattern {serverModel.GitIgnorePatternName} is missing on server";
    }

    private static string? GetDownloadProblem(StsGitDataModel serverGit, List<string> clientPatternNames)
    {
        return clientPatternNames.Contains(serverGit.GitIgnorePatternName, StringComparer.OrdinalIgnoreCase)
            ? null
            : $".gitignore pattern {serverGit.GitIgnorePatternName} is missing on client";
    }

    //კლიენტზე შაბლონის სახელი რეგისტრის გათვალისწინებით მოწმდება, ამიტომ კლიენტის ჩანაწერი გამოიყენება
    private static string GetClientPatternName(StsGitDataModel serverGit, List<string> clientPatternNames)
    {
        return clientPatternNames.Find(x =>
            string.Equals(x, serverGit.GitIgnorePatternName, StringComparison.OrdinalIgnoreCase)) ??
               serverGit.GitIgnorePatternName;
    }

    private static void WriteRecord(string name, string state, string? uploadProblem, string? downloadProblem,
        string[] usages)
    {
        List<string> parts = [state];
        if (uploadProblem is not null)
        {
            parts.Add($"{uploadProblem} - cannot be uploaded");
        }

        if (downloadProblem is not null)
        {
            parts.Add($"{downloadProblem} - cannot be downloaded");
        }

        if (usages.Length > 0)
        {
            parts.Add($"used by projects: {string.Join(", ", usages)} - cannot be deleted on client");
        }

        Console.WriteLine($"  {name}: {string.Join(", ", parts)}");
    }

    //არჩევანის გასწვრივ ნაჩვენები ინფორმაცია: რა მოხდება ჩანაწერებზე და რომელ მხარეს
    private static string DescribeChanges(string side, Changes changes, bool withDeletion)
    {
        List<string> parts = [];
        AddPart(parts, "add", changes.ToAdd);
        AddPart(parts, "update", changes.ToUpdate);
        if (withDeletion)
        {
            AddPart(parts, "delete", changes.ToDelete);
        }

        AddPart(parts, "cannot add", changes.NotToAdd);
        AddPart(parts, "cannot update", changes.NotToUpdate);
        if (withDeletion)
        {
            AddPart(parts, "cannot delete (in use)", changes.NotToDelete);
        }

        return parts.Count == 0 ? $"{side}: no changes" : $"{side}: {string.Join("; ", parts)}";
    }

    private static void AddPart(List<string> parts, string action, List<string> names)
    {
        if (names.Count > 0)
        {
            parts.Add($"{action} {string.Join(", ", names)}");
        }
    }

    //ერთი მიმართულების ცვლილებები: რა დაემატება, განახლდება და წაიშლება (Sync-ის დროს) და რა ვერა
    private sealed record Changes(
        List<string> ToAdd,
        List<string> ToUpdate,
        List<string> ToDelete,
        List<string> NotToAdd,
        List<string> NotToUpdate,
        List<string> NotToDelete);
}
