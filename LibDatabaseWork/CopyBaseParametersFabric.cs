using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParametersDataEdit.Models;
using DatabasesManagement;
using FileManagersMain;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibDatabaseWork.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibDatabaseWork;

public static class CopyBaseParametersFabric
{
    public static async Task<CopyBaseParameters?> CreateCopyBaseParameters(ILogger logger,
        IHttpClientFactory httpClientFactory, DatabasesParameters fromDatabaseParameters,
        DatabasesParameters toDatabaseParameters, SupportToolsParameters supportToolsParameters)
    {
        var databasesBackupFilesExchangeParameters = supportToolsParameters.DatabasesBackupFilesExchangeParameters;

        FileStorages fileStorages = new(supportToolsParameters.FileStorages);
        ApiClients apiClients = new(supportToolsParameters.ApiClients);
        DatabaseServerConnections databaseServerConnections = new(supportToolsParameters.DatabaseServerConnections);

        var localPath = databasesBackupFilesExchangeParameters?.LocalPath;

        if (string.IsNullOrWhiteSpace(localPath))
        {
            StShared.WriteErrorLine("localPath does not specified in databasesBackupFilesExchangeParameters", true);
            return null;
        }

        //1. თუ ლოკალური ფოლდერი არ არსებობს, შეიქმნას
        if (!Directory.Exists(localPath))
        {
            logger.LogInformation("Creating local folder {localPath}", localPath);
            Directory.CreateDirectory(localPath);
        }

        var sourceDbConnectionName = fromDatabaseParameters.DbConnectionName;
        var backupBaseName = fromDatabaseParameters.DatabaseName;

        var destinationDbConnectionName = toDatabaseParameters.DbConnectionName;
        var destinationBaseName = toDatabaseParameters.DatabaseName;

        //თუ წყარო და მიზანი ერთიდაიგივე არ არის ბაზების სახელები განსხვავებული უნდა იყოს.
        //ფაქტობრივად იმავე სერვერზე მოხდება ბაზის დაკოპირება
        if (sourceDbConnectionName == destinationDbConnectionName && backupBaseName == destinationBaseName)
        {
            logger.LogError("if source and destination servers are same, base names must be different");
            return null;
        }

        //თუ გაცვლის სერვერის პარამეტრები გვაქვს,
        //შევქმნათ შესაბამისი ფაილმენეჯერი
        var exchangeFileStorageName = databasesBackupFilesExchangeParameters?.ExchangeFileStorageName;
        Console.Write($" exchangeFileStorage - {exchangeFileStorageName}");
        var (exchangeFileStorage, exchangeFileManager) = await FileManagersFabricExt.CreateFileStorageAndFileManager(
            true, logger, localPath, exchangeFileStorageName, fileStorages, null, null, CancellationToken.None);

        //წყაროს ფაილსაცავი
        var sourceFileStorageName = fromDatabaseParameters.FileStorageName;

        var (sourceFileStorage, sourceFileManager) = await FileManagersFabricExt.CreateFileStorageAndFileManager(true,
            logger, localPath, sourceFileStorageName, fileStorages, null, null, CancellationToken.None);

        if (sourceFileManager == null)
        {
            logger.LogError("sourceFileManager does Not Created");
            return null;
        }

        if (sourceFileStorage == null)
        {
            logger.LogError("sourceFileStorage does Not Created");
            return null;
        }

        //მიზნის ფაილსაცავი
        var destinationFileStorageName = toDatabaseParameters.FileStorageName;

        Console.Write($" destinationFileStorage - {destinationFileStorageName}");
        var (destinationFileStorage, destinationFileManager) =
            await FileManagersFabricExt.CreateFileStorageAndFileManager(true, logger, localPath,
                destinationFileStorageName, fileStorages, null, null, CancellationToken.None);

        if (destinationFileStorage == null)
        {
            logger.LogError("destinationFileStorage does Not Created");
            return null;
        }

        if (destinationFileManager == null)
        {
            logger.LogError("destinationFileManager Not Created");
            return null;
        }

        Console.WriteLine();

        var localFileManager = FileManagersFabric.CreateFileManager(true, logger, localPath);

        if (localFileManager == null)
        {
            logger.LogError("localFileManager does not created");
            return null;
        }

        //sourceDbWebAgentName
        //პარამეტრების მიხედვით ბაზის სარეზერვო ასლის დამზადება და მოქაჩვა
        //წყაროს სერვერის აგენტის შექმნა
        var databaseManagerForSource = await DatabaseManagersFabric.CreateDatabaseManager(logger, httpClientFactory,
            true, sourceDbConnectionName, databaseServerConnections, apiClients, null, null, CancellationToken.None);

        if (databaseManagerForSource is null)
        {
            logger.LogError("Can not create client for source Database server");
            return null;
        }

        //destinationDbWebAgentName
        var databaseManagerForDestination = await DatabaseManagersFabric.CreateDatabaseManager(logger,
            httpClientFactory, true, destinationDbConnectionName, databaseServerConnections, apiClients, null, null,
            CancellationToken.None);

        if (databaseManagerForDestination is null)
        {
            logger.LogError("Can not create client for destination Database server");
            return null;
        }

        var sourceDatabaseName = fromDatabaseParameters.DatabaseName;
        //fromProductionToDeveloper ? dep.CurrentProductionBaseName : dep.DeveloperBaseName;
        if (string.IsNullOrWhiteSpace(sourceDatabaseName))
        {
            logger.LogError("sourceDatabaseName does not detected");
            return null;
        }

        var destinationDatabaseName = toDatabaseParameters.DatabaseName;

        if (string.IsNullOrWhiteSpace(destinationDatabaseName))
        {
            logger.LogError("destinationDatabaseName does not detected");
            return null;
        }

        var needDownloadFromSource = !FileStorageData.IsSameToLocal(sourceFileStorage, localPath);

        SmartSchemas smartSchemas = new(supportToolsParameters.SmartSchemas);

        var sourceSmartSchemaName = fromDatabaseParameters.SmartSchemaName;

        var sourceSmartSchema = string.IsNullOrWhiteSpace(sourceSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(sourceSmartSchemaName);

        var localSmartSchemaName = databasesBackupFilesExchangeParameters?.LocalSmartSchemaName;

        var localSmartSchema = string.IsNullOrWhiteSpace(localSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(localSmartSchemaName);

        var needUploadToDestination = !FileStorageData.IsSameToLocal(destinationFileStorage, localPath) &&
                                      sourceFileStorageName != destinationFileStorageName;

        var destinationSmartSchemaName = toDatabaseParameters.SmartSchemaName;

        var destinationSmartSchema = string.IsNullOrWhiteSpace(destinationSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(destinationSmartSchemaName);

        var needDownloadFromExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                       !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath) &&
                                       exchangeFileStorageName != sourceFileStorageName;

        var exchangeSmartSchemaName = databasesBackupFilesExchangeParameters?.ExchangeSmartSchemaName;

        var exchangeSmartSchema = string.IsNullOrWhiteSpace(exchangeSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(exchangeSmartSchemaName);

        var needDownloadFromDestination = !FileStorageData.IsSameToLocal(destinationFileStorage, localPath);

        var needUploadDestinationToExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                              !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath) &&
                                              exchangeFileStorageName != destinationFileStorageName;

        if (string.IsNullOrWhiteSpace(fromDatabaseParameters.DbServerFoldersSetName))
        {
            logger.LogError("fromDatabaseParameters.DbServerFoldersSetName is not specified");
            return null;
        }

        if (string.IsNullOrWhiteSpace(toDatabaseParameters.DbServerFoldersSetName))
        {
            logger.LogError("toDatabaseParameters.DbServerFoldersSetName is not specified");
            return null;
        }

        var sourceBackupRestoreParameters = new BackupRestoreParameters(databaseManagerForSource, sourceFileManager,
            sourceSmartSchema, sourceDatabaseName, fromDatabaseParameters.DbServerFoldersSetName);

        var destinationBackupRestoreParameters = new BackupRestoreParameters(databaseManagerForDestination,
            destinationFileManager, destinationSmartSchema, destinationDatabaseName,
            toDatabaseParameters.DbServerFoldersSetName);


        return new CopyBaseParameters(sourceBackupRestoreParameters, destinationBackupRestoreParameters,
            exchangeFileManager, localFileManager, needDownloadFromSource, localSmartSchema, needUploadToDestination,
            needDownloadFromExchange, exchangeSmartSchema, needDownloadFromDestination, needUploadDestinationToExchange,
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.DownloadTempExtension)
                ? "down!"
                : databasesBackupFilesExchangeParameters.DownloadTempExtension,
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.UploadTempExtension)
                ? "up!"
                : databasesBackupFilesExchangeParameters.UploadTempExtension, localPath);
    }
}