using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.DatabasesManagement.Models;
using ToolsManagement.FileManagersMain;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace LibDatabaseWork.ToolCommands.RestoreDatabaseFromFileStorageToNewDatabase;

//ფაილსაცავში არსებული ბექაპის ფაილიდან ამ სერვერის ახალი ბაზის (NewDatabaseParameters) აღდგენა.
//ფაილსაცავი და ბექაპის ფაილი კონსოლიდან აირჩევა
// ReSharper disable once UnusedType.Global
public sealed class RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IApplication _app;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly Func<string, List<string>, string?, string?> _selectFromList;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy(IApplication app,
        ILogger<RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory) : this(app, logger, httpClientFactory, SelectFromList)
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    internal RestoreDatabaseFromFileStorageToNewDatabaseFactoryStrategy(IApplication app, ILogger logger,
        IHttpClientFactory httpClientFactory, Func<string, List<string>, string?, string?> selectFromList)
    {
        _app = app;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _selectFromList = selectFromList;
    }

    public string ToolCommandName => nameof(EProjectServerTools.RestoreDatabaseFromFileStorageToNewDatabase);

    public async ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;
        string serverKey = serverInfo.GetItemKey();

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        DatabaseParameters? newDatabaseParameters = serverInfo.NewDatabaseParameters;
        if (newDatabaseParameters is null)
        {
            StShared.WriteErrorLine($"NewDatabaseParameters is not specified {serverKey}", true);
            return null;
        }

        DatabasesBackupFilesExchangeParameters? databasesBackupFilesExchangeParameters =
            supportToolsParameters.DatabasesBackupFilesExchangeParameters;
        var fileStorages = new FileStorages(supportToolsParameters.FileStorages);

        //მიზნის (ახალი ბაზის) პარამეტრები
        var createBaseBackupParametersFactory =
            new CreateBaseBackupParametersFactory(_app.AppName, _logger, null, null, true);
        Result<BaseBackupParameters> createDestinationBaseBackupParametersResult =
            await createBaseBackupParametersFactory.CreateBaseBackupParameters(_httpClientFactory,
                newDatabaseParameters, new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections),
                new ApiClients(supportToolsParameters.ApiClients), fileStorages,
                new SmartSchemas(supportToolsParameters.SmartSchemas), databasesBackupFilesExchangeParameters,
                cancellationToken);
        Console.WriteLine();

        if (createDestinationBaseBackupParametersResult.IsFailure)
        {
            createDestinationBaseBackupParametersResult.Error.PrintErrorsOnConsole();
            return null;
        }

        BaseBackupParameters destinationBackupParameters = createDestinationBaseBackupParametersResult.Value;
        string localPath = destinationBackupParameters.LocalPath;

        //წყაროს ფაილსაცავი
        string? sourceFileStorageName = SelectSourceFileStorageName([.. supportToolsParameters.FileStorages.Keys],
            serverInfo.CurrentDatabaseParameters?.FileStorageName);
        if (sourceFileStorageName is null)
        {
            return null;
        }

        (FileStorageData? sourceFileStorage, FileManager? sourceFileManager) =
            await FileManagersFactoryExt.CreateFileStorageAndFileManager(true, _logger, localPath,
                sourceFileStorageName, fileStorages, null, null, cancellationToken);

        if (sourceFileStorage is null || sourceFileManager is null)
        {
            StShared.WriteErrorLine($"File manager for file storage {sourceFileStorageName} is not created", true);
            return null;
        }

        //ბექაპის ფაილი
        BackupFileParameters? backupFile = SelectBackupFile(sourceFileStorageName,
            GetBackupFiles(sourceFileManager.GetFileNames(null, null), destinationBackupParameters.DateMask));
        if (backupFile is null)
        {
            return null;
        }

        //თუ წყაროს და მიზნის (ან გაცვლის) ფაილსაცავები ემთხვევა, ფაილი იქ უკვე არის და ატვირთვა საჭირო აღარ არის
        bool needUploadToDestination =
            !FileStorageData.IsSameToLocal(destinationBackupParameters.BackupRestoreParameters.FileStorage,
                localPath) && sourceFileStorageName != newDatabaseParameters.FileStorageName;

        bool needUploadToExchange = destinationBackupParameters.NeedUploadToExchange && sourceFileStorageName !=
            databasesBackupFilesExchangeParameters?.ExchangeFileStorageName;

        var restoreFromFileStorageParameters = new RestoreFromFileStorageParameters(destinationBackupParameters,
            sourceFileManager, backupFile, !FileStorageData.IsSameToLocal(sourceFileStorage, localPath),
            needUploadToDestination, needUploadToExchange);

        Console.WriteLine(
            $"File {backupFile.Name} of file storage {sourceFileStorageName} will be restored to database {newDatabaseParameters.DatabaseName} of server {serverKey}");

        return new RestoreDatabaseFromFileStorageToNewDatabaseToolCommand(_logger, restoreFromFileStorageParameters,
            parametersManager);
    }

    internal string? SelectSourceFileStorageName(List<string> fileStorageNames, string? defaultFileStorageName)
    {
        if (fileStorageNames.Count == 0)
        {
            StShared.WriteErrorLine("There are no file storages to restore database from", true);
            return null;
        }

        string? fileStorageName = Select("Restore database from which file storage?",
            [.. fileStorageNames.Order(StringComparer.OrdinalIgnoreCase)],
            fileStorageNames.Find(x => x == defaultFileStorageName));

        if (fileStorageName is not null && fileStorageNames.Contains(fileStorageName))
        {
            return fileStorageName;
        }

        StShared.WriteErrorLine("File storage is not selected", true);
        return null;
    }

    internal BackupFileParameters? SelectBackupFile(string fileStorageName, List<BackupFileParameters> backupFiles)
    {
        if (backupFiles.Count == 0)
        {
            StShared.WriteErrorLine($"File storage {fileStorageName} has no files", true);
            return null;
        }

        string? fileName = Select($"Restore which backup file of file storage {fileStorageName}?",
            [.. backupFiles.Select(x => x.Name)], null);

        BackupFileParameters? backupFile = backupFiles.Find(x => x.Name == fileName);
        if (backupFile is not null)
        {
            return backupFile;
        }

        StShared.WriteErrorLine("Backup file is not selected", true);
        return null;
    }

    //ფაილსაცავის ყველა ფაილი, სახელის კლებით (სახელებში თარიღია, ასე ყველაზე ახალი ზემოთაა).
    //აღდგენა მხოლოდ სახელს იყენებს, მაგრამ WebAgent-ს ცარიელი პრეფიქსი, სუფიქსი და თარიღის ფორმატი არ მიაქვს,
    //ამიტომ პრეფიქსია სახელი გაფართოების გარეშე, სუფიქსი კი გაფართოება (თუ ის არ აქვს, მთელი სახელი)
    internal static List<BackupFileParameters> GetBackupFiles(IEnumerable<string> fileNames, string dateMask)
    {
        return
        [
            .. fileNames.Order(StringComparer.OrdinalIgnoreCase).Reverse().Select(fileName =>
            {
                string prefix = Path.GetFileNameWithoutExtension(fileName);
                string suffix = Path.GetExtension(fileName);
                return new BackupFileParameters(null, fileName, prefix.Length == 0 ? fileName : prefix,
                    suffix.Length == 0 ? fileName : suffix, dateMask);
            })
        ];
    }

    private string? Select(string caption, List<string> items, string? defaultValue)
    {
        try
        {
            return _selectFromList(caption, items, defaultValue);
        }
        catch (DataInputException)
        {
            return null;
        }
    }

    private static string? SelectFromList(string fieldName, List<string> items, string? defaultValue)
    {
        var menuSet = new CliMenuSet();
        foreach (string item in items)
        {
            menuSet.AddMenuItem(new CliMenuCommand(item));
        }

        return MenuInputer.InputFromMenuList(fieldName, menuSet, defaultValue);
    }
}
