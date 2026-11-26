using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.DatabasesManagement.Models;
using ToolsManagement.FileManagersMain;

namespace LibDatabaseWork;

public static class CopyBaseParametersFactory
{
    public static async Task<CopyBaseParameters?> CreateCopyBaseParameters(string appName, ILogger logger,
        IHttpClientFactory httpClientFactory, DatabaseParameters fromDatabaseParameters,
        DatabaseParameters toDatabaseParameters, SupportToolsParameters supportToolsParameters,
        CancellationToken cancellationToken = default)
    {
        //CreateBaseBackupParameters ორივე მხარეს ამოწმებს, მაგრამ შეცდომაში მხარეს არ წერს (მიზანსაც
        //fromDatabaseParameters-ს უწოდებს), ამიტომ აქ, მანამდე ვამოწმებთ და ორივე მხარის ყველა ნაკლს ერთად ვწერთ
        bool sourceIsValid = CheckDatabaseParameters(logger, fromDatabaseParameters, "source");
        bool destinationIsValid = CheckDatabaseParameters(logger, toDatabaseParameters, "destination");
        if (!sourceIsValid || !destinationIsValid)
        {
            return null;
        }

        DatabasesBackupFilesExchangeParameters? databasesBackupFilesExchangeParameters =
            supportToolsParameters.DatabasesBackupFilesExchangeParameters;
        string? exchangeFileStorageName = databasesBackupFilesExchangeParameters?.ExchangeFileStorageName;

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);
        var apiClients = new ApiClients(supportToolsParameters.ApiClients);
        var fileStorages = new FileStorages(supportToolsParameters.FileStorages);
        var smartSchemas = new SmartSchemas(supportToolsParameters.SmartSchemas);

        var createSourceBaseBackupParametersFactory =
            new CreateBaseBackupParametersFactory(appName, logger, null, null, true);
        Result<BaseBackupParameters> createSourceBaseBackupParametersResult =
            await createSourceBaseBackupParametersFactory.CreateBaseBackupParameters(httpClientFactory,
                fromDatabaseParameters, databaseServerConnections, apiClients, fileStorages, smartSchemas,
                databasesBackupFilesExchangeParameters, cancellationToken);

        if (createSourceBaseBackupParametersResult.IsFailure)
        {
            createSourceBaseBackupParametersResult.Error.PrintErrorsOnConsole();
            return null;
        }

        var createDestinationBaseBackupParametersFactory =
            new CreateBaseBackupParametersFactory(appName, logger, null, null, true);
        Result<BaseBackupParameters> createDestinationBaseBackupParametersResult =
            await createDestinationBaseBackupParametersFactory.CreateBaseBackupParameters(httpClientFactory,
                toDatabaseParameters, databaseServerConnections, apiClients, fileStorages, smartSchemas,
                databasesBackupFilesExchangeParameters, cancellationToken);

        if (createDestinationBaseBackupParametersResult.IsFailure)
        {
            createDestinationBaseBackupParametersResult.Error.PrintErrorsOnConsole();
            return null;
        }

        //CreateBaseBackupParameters-მა ორივე მხარის ბაზის მენეჯერი, ფაილსაცავი და ლოკალური ფოლდერი უკვე შექმნა და
        //შეამოწმა, ამიტომ ლოკალური ფოლდერი და მიზნის ფაილსაცავი იქიდან აიღება
        BaseBackupParameters sourceBackupParameters = createSourceBaseBackupParametersResult.Value;
        BaseBackupParameters destinationBackupParameters = createDestinationBaseBackupParametersResult.Value;
        string localPath = sourceBackupParameters.LocalPath;

        string? sourceDbConnectionName = fromDatabaseParameters.DbConnectionName;
        string? backupBaseName = fromDatabaseParameters.DatabaseName;

        string? destinationDbConnectionName = toDatabaseParameters.DbConnectionName;
        string? destinationBaseName = toDatabaseParameters.DatabaseName;

        //თუ წყარო და მიზანი ერთიდაიგივე არ არის ბაზების სახელები განსხვავებული უნდა იყოს.
        //ფაქტობრივად იმავე სერვერზე მოხდება ბაზის დაკოპირება
        if (sourceDbConnectionName == destinationDbConnectionName && backupBaseName == destinationBaseName)
        {
            logger.LogError("if source and destination servers are same, base names must be different");
            return null;
        }

        //თუ გაცვლის სერვერის პარამეტრები გვაქვს,
        //შევქმნათ შესაბამისი ფაილმენეჯერი
        Console.Write($" exchangeFileStorage - {exchangeFileStorageName}");
        (FileStorageData? exchangeFileStorage, FileManager? exchangeFileManager) =
            await FileManagersFactoryExt.CreateFileStorageAndFileManager(true, logger, localPath,
                exchangeFileStorageName, fileStorages, null, null, cancellationToken);

        string? sourceFileStorageName = fromDatabaseParameters.FileStorageName;

        //მიზნის ფაილსაცავი
        string? destinationFileStorageName = toDatabaseParameters.FileStorageName;
        FileStorageData destinationFileStorage = destinationBackupParameters.BackupRestoreParameters.FileStorage;

        Console.WriteLine($" destinationFileStorage - {destinationFileStorageName}");

        bool needUploadToDestination = !FileStorageData.IsSameToLocal(destinationFileStorage, localPath) &&
                                       sourceFileStorageName != destinationFileStorageName;

        bool needDownloadFromExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                        !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath) &&
                                        exchangeFileStorageName != sourceFileStorageName;

        string? exchangeSmartSchemaName = databasesBackupFilesExchangeParameters?.ExchangeSmartSchemaName;

        //GetSmartSchemaByKey ცარიელ სახელზე null-ს აბრუნებს
        SmartSchema? exchangeSmartSchema = smartSchemas.GetSmartSchemaByKey(exchangeSmartSchemaName);

        return new CopyBaseParameters(sourceBackupParameters, destinationBackupParameters, exchangeFileManager,
            needUploadToDestination, needDownloadFromExchange, exchangeSmartSchema,
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.UploadTempExtension)
                ? "up!"
                : databasesBackupFilesExchangeParameters.UploadTempExtension);
    }

    //CreateBaseBackupParameters-ის სავალდებულო ველები. შეცდომაში მხარე და, თუ ცნობილია, ბაზის სახელი იწერება
    private static bool CheckDatabaseParameters(ILogger logger, DatabaseParameters databaseParameters, string side)
    {
        string database = string.IsNullOrWhiteSpace(databaseParameters.DatabaseName)
            ? string.Empty
            : $" (database {databaseParameters.DatabaseName})";

        //ყველა ველი მოწმდება, რომ ყველა ნაკლი ერთად ჩანდეს
        bool isValid = true;
        CheckField(nameof(DatabaseParameters.DbConnectionName), databaseParameters.DbConnectionName);
        CheckField(nameof(DatabaseParameters.DatabaseName), databaseParameters.DatabaseName);
        CheckField(nameof(DatabaseParameters.DbServerFoldersSetName), databaseParameters.DbServerFoldersSetName);
        CheckField(nameof(DatabaseParameters.FileStorageName), databaseParameters.FileStorageName);
        return isValid;

        void CheckField(string fieldName, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            logger.LogError("{FieldName} is not specified in {Side} database parameters{Database}", fieldName, side,
                database);
            isValid = false;
        }
    }
}
