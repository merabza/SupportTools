using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibMenuInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands.GitIgnoreFileTypes;

//კლიენტის (GitIgnorePatterns + ფაილები) და სერვერის .gitignore ჩანაწერების შედარება და სინქრონიზაცია არჩეული მიმართულებით.
//Merge ზედმეტ ჩანაწერებს არ შლის, Sync შლის, მაგრამ მხოლოდ იმათ, რომლებსაც იმავე მხარეს არც ერთი git რეპოზიტორია არ იყენებს
public sealed class SyncGitignoreFilesCliMenuCommand : CliMenuCommand
{
    private const int MergeUp = 0;
    private const int SyncUp = 1;
    private const int MergeDown = 2;
    private const int SyncDown = 3;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Func<string, CliMenuSet, int> _inputIdFromMenuList;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public SyncGitignoreFilesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        (fieldName, listSet) => MenuInputer.InputIdFromMenuList(fieldName, listSet))
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncGitignoreFilesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, Func<string, CliMenuSet, int> inputIdFromMenuList) : base(
        "Sync .gitignore files...", EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _inputIdFromMenuList = inputIdFromMenuList;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? folderForGitignoreFiles = parameters.FolderForGitignoreFiles;
        if (string.IsNullOrWhiteSpace(folderForGitignoreFiles))
        {
            StShared.WriteErrorLine("supportToolsParameters.FolderForGitignoreFiles is empty", true, _logger);
            return false;
        }

        //კლიენტის ჩანაწერი სახელით (GitIgnorePatterns) და ფაილის შიგთავსით
        var clientRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string gitIgnoreModelName in parameters.GitIgnorePatterns)
        {
            string fileName =
                SupportToolsParameters.GetGitIgnoreModelFilePath(folderForGitignoreFiles, gitIgnoreModelName);
            if (!File.Exists(fileName))
            {
                StShared.WriteErrorLine($".gitignore file {fileName} does not exists", true, _logger);
                return false;
            }

            clientRecords[gitIgnoreModelName] = await File.ReadAllTextAsync(fileName, cancellationToken);
        }

        SupportToolsServerApiClient? supportToolsServerApiClient =
            parameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true, _logger);
            return false;
        }

        Result<List<StsGitIgnoreFileTypeDataModel>> serverRecordsResult =
            await supportToolsServerApiClient.GetGitIgnoreFileTypesList(cancellationToken);
        if (serverRecordsResult.IsFailure)
        {
            serverRecordsResult.Error.PrintErrorsOnConsole();
            return false;
        }

        //სერვერის git რეპოზიტორიები საჭიროა იმის დასადგენად, შეიძლება თუ არა სერვერზე ზედმეტი ჩანაწერის წაშლა
        Result<List<StsGitDataModel>> serverGitsResult =
            await supportToolsServerApiClient.GetGitRepos(cancellationToken);
        if (serverGitsResult.IsFailure)
        {
            serverGitsResult.Error.PrintErrorsOnConsole();
            return false;
        }

        Dictionary<string, string> serverRecords = serverRecordsResult.Value.ToDictionary(x => x.Name,
            x => x.Content, StringComparer.OrdinalIgnoreCase);

        List<string> onlyOnClient = [.. clientRecords.Keys.Where(x => !serverRecords.ContainsKey(x)).Order()];
        List<string> onlyOnServer = [.. serverRecords.Keys.Where(x => !clientRecords.ContainsKey(x)).Order()];
        List<string> different =
        [
            .. clientRecords.Where(x =>
                serverRecords.TryGetValue(x.Key, out string? serverContent) &&
                !string.Equals(x.Value, serverContent, StringComparison.Ordinal)).Select(x => x.Key).Order()
        ];

        if (onlyOnClient.Count == 0 && onlyOnServer.Count == 0 && different.Count == 0)
        {
            Console.WriteLine("Client and server .gitignore records are identical, nothing to sync");
            return true;
        }

        //ზედმეტი ჩანაწერის წაშლა შეუძლებელია, თუ მას იმავე მხარეს რომელიმე git რეპოზიტორია იყენებს
        Dictionary<string, string[]> serverUsages = onlyOnServer.ToDictionary(x => x, x => (string[])
        [
            .. serverGitsResult.Value
                .Where(g => string.Equals(g.GitIgnorePatternName, x, StringComparison.OrdinalIgnoreCase))
                .Select(g => g.GitProjectName).Order()
        ]);
        Dictionary<string, string[]> clientUsages = onlyOnClient.ToDictionary(x => x, x => (string[])
        [
            .. parameters.Gits
                .Where(g => string.Equals(g.Value.GitIgnorePatternName, x, StringComparison.OrdinalIgnoreCase))
                .Select(g => g.Key).Order()
        ]);

        Console.WriteLine("Differences between client and server .gitignore records:");
        foreach (string name in different)
        {
            Console.WriteLine($"  {name}: content differs");
        }

        foreach (string name in onlyOnClient)
        {
            Console.WriteLine($"  {name}: only on client{DescribeUsage(clientUsages[name], "client")}");
        }

        foreach (string name in onlyOnServer)
        {
            Console.WriteLine($"  {name}: only on server{DescribeUsage(serverUsages[name], "server")}");
        }

        Console.WriteLine();

        List<string> deletableOnServer = [.. onlyOnServer.Where(x => serverUsages[x].Length == 0)];
        List<string> notDeletableOnServer = [.. onlyOnServer.Where(x => serverUsages[x].Length > 0)];
        List<string> deletableOnClient = [.. onlyOnClient.Where(x => clientUsages[x].Length == 0)];
        List<string> notDeletableOnClient = [.. onlyOnClient.Where(x => clientUsages[x].Length > 0)];

        var processesMenuSet = new CliMenuSet();
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Merge Up",
            DescribeChanges("server", onlyOnClient, different, [], [])));
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Sync Up",
            DescribeChanges("server", onlyOnClient, different, deletableOnServer, notDeletableOnServer)));
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Merge Down",
            DescribeChanges("client", onlyOnServer, different, [], [])));
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Sync Down",
            DescribeChanges("client", onlyOnServer, different, deletableOnClient, notDeletableOnClient)));

        int process = _inputIdFromMenuList("process to run", processesMenuSet);

        switch (process)
        {
            case MergeUp:
            case SyncUp:
                return await Upload(supportToolsServerApiClient, clientRecords, [.. onlyOnClient, .. different],
                    process == SyncUp ? deletableOnServer : [], process == SyncUp ? notDeletableOnServer : [],
                    cancellationToken);
            case MergeDown:
            case SyncDown:
                return await Download(parameters, folderForGitignoreFiles, serverRecordsResult.Value,
                    [.. onlyOnServer, .. different], process == SyncDown ? deletableOnClient : [],
                    process == SyncDown ? notDeletableOnClient : [], cancellationToken);
            default:
                StShared.WriteErrorLine("Selected invalid process", true, _logger);
                return false;
        }
    }

    private async Task<bool> Upload(SupportToolsServerApiClient supportToolsServerApiClient,
        Dictionary<string, string> clientRecords, List<string> namesToUpload, List<string> namesToDelete,
        List<string> namesNotToDelete, CancellationToken cancellationToken)
    {
        if (namesToUpload.Count > 0)
        {
            //merge=true-ს დროს სერვერი მხოლოდ ამატებს და ანახლებს. სერვერი არსებულ ჩანაწერს სახელით პოულობს,
            //ამიტომ კლიენტის მიერ გამოგზავნილი Id არ გამოიყენება
            Result uploadResult = await supportToolsServerApiClient.SyncUpGitIgnoreFileTypes([
                .. namesToUpload.Select(x => new StsGitIgnoreFileTypeDataModel
                {
                    Id = Guid.NewGuid(), Name = x, Content = clientRecords[x]
                })
            ], true, cancellationToken);
            if (uploadResult.IsFailure)
            {
                uploadResult.Error.PrintErrorsOnConsole();
                return false;
            }
        }

        //სერვერი წაშლის წინ გამოყენებას თავიდანაც ამოწმებს
        foreach (string name in namesToDelete)
        {
            Result deleteResult =
                await supportToolsServerApiClient.RemoveGitIgnoreFileTypeName(name, cancellationToken);
            if (deleteResult.IsFailure)
            {
                deleteResult.Error.PrintErrorsOnConsole();
                return false;
            }
        }

        WriteNotDeleted(namesNotToDelete, "server");
        Console.WriteLine(
            $"{namesToUpload.Count} .gitignore records uploaded to server, {namesToDelete.Count} deleted from server");
        return true;
    }

    private async Task<bool> Download(SupportToolsParameters parameters, string folderForGitignoreFiles,
        List<StsGitIgnoreFileTypeDataModel> serverRecords, List<string> namesToDownload, List<string> namesToDelete,
        List<string> namesNotToDelete, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folderForGitignoreFiles);
        foreach (string name in namesToDownload)
        {
            StsGitIgnoreFileTypeDataModel serverRecord =
                serverRecords.First(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            //თუ ჩანაწერი კლიენტზე უკვე არის, მისი სახელი უცვლელი რჩება
            string? clientName = parameters.GitIgnorePatterns.Find(x =>
                string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
            if (clientName is null)
            {
                clientName = serverRecord.Name;
                parameters.GitIgnorePatterns.Add(clientName);
            }

            await File.WriteAllTextAsync(
                SupportToolsParameters.GetGitIgnoreModelFilePath(folderForGitignoreFiles, clientName),
                serverRecord.Content, cancellationToken);
        }

        foreach (string name in namesToDelete)
        {
            parameters.GitIgnorePatterns.Remove(name);
            File.Delete(SupportToolsParameters.GetGitIgnoreModelFilePath(folderForGitignoreFiles, name));
        }

        if (!await _parametersManager.Save(parameters, ".gitignore records synced from server", null,
                cancellationToken))
        {
            return false;
        }

        WriteNotDeleted(namesNotToDelete, "client");
        Console.WriteLine(
            $"{namesToDownload.Count} .gitignore records downloaded from server, {namesToDelete.Count} deleted from client");
        return true;
    }

    private void WriteNotDeleted(List<string> names, string side)
    {
        if (names.Count > 0)
        {
            StShared.WriteWarningLine(
                $"Records used by git repositories were not deleted from {side}: {string.Join(", ", names)}", true,
                _logger);
        }
    }

    private static string DescribeUsage(string[] gitNames, string side)
    {
        return gitNames.Length == 0
            ? string.Empty
            : $", used by {side} gits: {string.Join(", ", gitNames)} - cannot be deleted on {side}";
    }

    //არჩევანის გასწვრივ ნაჩვენები ინფორმაცია: რა მოხდება ჩანაწერებზე და რომელ მხარეს
    private static string DescribeChanges(string side, List<string> toAdd, List<string> toUpdate, List<string> toDelete,
        List<string> notToDelete)
    {
        List<string> parts = [];
        AddPart(parts, "add", toAdd);
        AddPart(parts, "update", toUpdate);
        AddPart(parts, "delete", toDelete);
        AddPart(parts, "cannot delete (in use)", notToDelete);
        return parts.Count == 0 ? $"{side}: no changes" : $"{side}: {string.Join("; ", parts)}";
    }

    private static void AddPart(List<string> parts, string action, List<string> names)
    {
        if (names.Count > 0)
        {
            parts.Add($"{action} {string.Join(", ", names)}");
        }
    }
}
