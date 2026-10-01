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

namespace SupportTools.CliMenuCommands;

//კლიენტის (EditorConfigPatterns + ფაილები) და სერვერის .editorconfig ჩანაწერების შედარება და სინქრონიზაცია არჩეული
//მიმართულებით, ისევე როგორც .gitignore ჩანაწერებისთვის. Merge ზედმეტ ჩანაწერებს არ შლის, Sync შლის.
//სერვერზე .editorconfig ჩანაწერს არაფერი იყენებს, ამიტომ იქ ყოველთვის იშლება. კლიენტზე იშლება მხოლოდ ის,
//რომელსაც არც ერთი პროექტი არ იყენებს
public sealed class SyncEditorConfigFilesCliMenuCommand : CliMenuCommand
{
    private const int MergeUp = 0;
    private const int SyncUp = 1;
    private const int MergeDown = 2;
    private const int SyncDown = 3;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Func<string, CliMenuSet, int> _inputIdFromMenuList;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public SyncEditorConfigFilesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        (fieldName, listSet) => MenuInputer.InputIdFromMenuList(fieldName, listSet))
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncEditorConfigFilesCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, Func<string, CliMenuSet, int> inputIdFromMenuList) : base(
        "Sync .editorconfig files...", EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _inputIdFromMenuList = inputIdFromMenuList;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? folderForEditorConfigFiles = parameters.FolderForEditorConfigFiles;
        if (string.IsNullOrWhiteSpace(folderForEditorConfigFiles))
        {
            StShared.WriteErrorLine("supportToolsParameters.FolderForEditorConfigFiles is empty", true, _logger);
            return false;
        }

        //კლიენტის ჩანაწერი სახელით (EditorConfigPatterns) და ფაილის შიგთავსით
        var clientRecords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string editorConfigPatternName in parameters.EditorConfigPatterns)
        {
            string fileName =
                SupportToolsParameters.GetEditorConfigPatternFilePath(folderForEditorConfigFiles,
                    editorConfigPatternName);
            if (!File.Exists(fileName))
            {
                StShared.WriteErrorLine($".editorconfig file {fileName} does not exist", true, _logger);
                return false;
            }

            clientRecords[editorConfigPatternName] = await File.ReadAllTextAsync(fileName, cancellationToken);
        }

        SupportToolsServerApiClient? supportToolsServerApiClient =
            parameters.GetSupportToolsServerApiClient(_logger, _httpClientFactory);
        if (supportToolsServerApiClient is null)
        {
            StShared.WriteErrorLine("supportToolsServerApiClient is null", true, _logger);
            return false;
        }

        Result<List<StsEditorConfigFileTypeDataModel>> serverRecordsResult =
            await supportToolsServerApiClient.GetEditorConfigFileTypesList(cancellationToken);
        if (serverRecordsResult.IsFailure)
        {
            serverRecordsResult.Error.PrintErrorsOnConsole();
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
            Console.WriteLine("Client and server .editorconfig records are identical, nothing to sync");
            return true;
        }

        //კლიენტზე ზედმეტი ჩანაწერის წაშლა შეუძლებელია, თუ მას რომელიმე პროექტი იყენებს
        Dictionary<string, string[]> clientUsages = onlyOnClient.ToDictionary(x => x, x => (string[])
        [
            .. parameters.Projects
                .Where(p => string.Equals(p.Value.EditorConfigPatternName, x, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Key).Order()
        ]);

        Console.WriteLine("Differences between client and server .editorconfig records:");
        foreach (string name in different)
        {
            Console.WriteLine($"  {name}: content differs");
        }

        foreach (string name in onlyOnClient)
        {
            Console.WriteLine(clientUsages[name].Length == 0
                ? $"  {name}: only on client"
                : $"  {name}: only on client, used by projects: {string.Join(", ", clientUsages[name])} - cannot be deleted on client");
        }

        foreach (string name in onlyOnServer)
        {
            Console.WriteLine($"  {name}: only on server");
        }

        Console.WriteLine();

        List<string> deletableOnClient = [.. onlyOnClient.Where(x => clientUsages[x].Length == 0)];
        List<string> notDeletableOnClient = [.. onlyOnClient.Where(x => clientUsages[x].Length > 0)];

        var processesMenuSet = new CliMenuSet();
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Merge Up",
            DescribeChanges("server", onlyOnClient, different, [], [])));
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Sync Up",
            DescribeChanges("server", onlyOnClient, different, onlyOnServer, [])));
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Merge Down",
            DescribeChanges("client", onlyOnServer, different, [], [])));
        processesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Sync Down",
            DescribeChanges("client", onlyOnServer, different, deletableOnClient, notDeletableOnClient)));

        int process = _inputIdFromMenuList("process to run", processesMenuSet);

        switch (process)
        {
            case MergeUp:
                //merge=true-ს დროს სერვერი მხოლოდ ამატებს და ანახლებს
                return await Upload(supportToolsServerApiClient, clientRecords, [.. onlyOnClient, .. different], true,
                    0, cancellationToken);
            case SyncUp:
                //merge=false-ის დროს სერვერი ატვირთულ სიაში არარსებულ ჩანაწერებს შლის, ამიტომ მიდის სრული სია
                return await Upload(supportToolsServerApiClient, clientRecords, [.. clientRecords.Keys], false,
                    onlyOnServer.Count, cancellationToken);
            case MergeDown:
            case SyncDown:
                return await Download(parameters, folderForEditorConfigFiles, serverRecordsResult.Value,
                    [.. onlyOnServer, .. different], process == SyncDown ? deletableOnClient : [],
                    process == SyncDown ? notDeletableOnClient : [], cancellationToken);
            default:
                StShared.WriteErrorLine("Selected invalid process", true, _logger);
                return false;
        }
    }

    private static async Task<bool> Upload(SupportToolsServerApiClient supportToolsServerApiClient,
        Dictionary<string, string> clientRecords, List<string> namesToUpload, bool merge, int deletedCount,
        CancellationToken cancellationToken)
    {
        if (namesToUpload.Count > 0 || !merge)
        {
            Result uploadResult = await supportToolsServerApiClient.SyncUpEditorConfigFileTypes([
                .. namesToUpload.Select(x => new StsEditorConfigFileTypeDataModel
                {
                    Name = x, Content = clientRecords[x]
                })
            ], merge, cancellationToken);
            if (uploadResult.IsFailure)
            {
                uploadResult.Error.PrintErrorsOnConsole();
                return false;
            }
        }

        Console.WriteLine(
            $"{namesToUpload.Count} .editorconfig records uploaded to server, {deletedCount} deleted from server");
        return true;
    }

    private async Task<bool> Download(SupportToolsParameters parameters, string folderForEditorConfigFiles,
        List<StsEditorConfigFileTypeDataModel> serverRecords, List<string> namesToDownload, List<string> namesToDelete,
        List<string> namesNotToDelete, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(folderForEditorConfigFiles);
        foreach (string name in namesToDownload)
        {
            StsEditorConfigFileTypeDataModel serverRecord =
                serverRecords.First(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            //თუ ჩანაწერი კლიენტზე უკვე არის, მისი სახელი უცვლელი რჩება
            string? clientName = parameters.EditorConfigPatterns.Find(x =>
                string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
            if (clientName is null)
            {
                clientName = serverRecord.Name;
                parameters.EditorConfigPatterns.Add(clientName);
            }

            await File.WriteAllTextAsync(
                SupportToolsParameters.GetEditorConfigPatternFilePath(folderForEditorConfigFiles, clientName),
                serverRecord.Content, cancellationToken);
        }

        foreach (string name in namesToDelete)
        {
            parameters.EditorConfigPatterns.Remove(name);
            File.Delete(SupportToolsParameters.GetEditorConfigPatternFilePath(folderForEditorConfigFiles, name));
        }

        if (!await _parametersManager.Save(parameters, ".editorconfig records synced from server", null,
                cancellationToken))
        {
            return false;
        }

        if (namesNotToDelete.Count > 0)
        {
            StShared.WriteWarningLine(
                $"Records used by projects were not deleted from client: {string.Join(", ", namesNotToDelete)}", true,
                _logger);
        }

        Console.WriteLine(
            $"{namesToDownload.Count} .editorconfig records downloaded from server, {namesToDelete.Count} deleted from client");
        return true;
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
