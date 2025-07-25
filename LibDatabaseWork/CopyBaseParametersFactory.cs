﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DatabasesManagement;
using FileManagersMain;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibDatabaseWork.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace LibDatabaseWork;

public static class CopyBaseParametersFactory
{
    public static async Task<CopyBaseParameters?> CreateCopyBaseParameters(ILogger logger,
        IHttpClientFactory httpClientFactory, DatabaseParameters fromDatabaseParameters,
        DatabaseParameters toDatabaseParameters, SupportToolsParameters supportToolsParameters,
        CancellationToken cancellationToken = default)
    {
        var databasesBackupFilesExchangeParameters = supportToolsParameters.DatabasesBackupFilesExchangeParameters;
        var localPath = databasesBackupFilesExchangeParameters?.LocalPath;
        var exchangeFileStorageName = databasesBackupFilesExchangeParameters?.ExchangeFileStorageName;

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);
        var apiClients = new ApiClients(supportToolsParameters.ApiClients);
        var fileStorages = new FileStorages(supportToolsParameters.FileStorages);
        var smartSchemas = new SmartSchemas(supportToolsParameters.SmartSchemas);

        var createSourceBaseBackupParametersFactory = new CreateBaseBackupParametersFactory(logger, null, null, true);
        var createSourceBaseBackupParametersResult =
            await createSourceBaseBackupParametersFactory.CreateBaseBackupParameters(httpClientFactory,
                fromDatabaseParameters, databaseServerConnections, apiClients, fileStorages, smartSchemas,
                databasesBackupFilesExchangeParameters, cancellationToken);

        if (createSourceBaseBackupParametersResult.IsT1)
        {
            Err.PrintErrorsOnConsole(createSourceBaseBackupParametersResult.AsT1);
            return null;
        }

        var createDestinationBaseBackupParametersFactory =
            new CreateBaseBackupParametersFactory(logger, null, null, true);
        var createDestinationBaseBackupParametersResult =
            await createDestinationBaseBackupParametersFactory.CreateBaseBackupParameters(httpClientFactory,
                toDatabaseParameters, databaseServerConnections, apiClients, fileStorages, smartSchemas,
                databasesBackupFilesExchangeParameters, cancellationToken);

        if (createDestinationBaseBackupParametersResult.IsT1)
        {
            Err.PrintErrorsOnConsole(createDestinationBaseBackupParametersResult.AsT1);
            return null;
        }

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
        Console.Write($" exchangeFileStorage - {exchangeFileStorageName}");
        var (exchangeFileStorage, exchangeFileManager) = await FileManagersFactoryExt.CreateFileStorageAndFileManager(
            true, logger, localPath, exchangeFileStorageName, fileStorages, null, null, cancellationToken);

        //წყაროს ფაილსაცავი
        var sourceFileStorageName = fromDatabaseParameters.FileStorageName;

        var (sourceFileStorage, sourceFileManager) = await FileManagersFactoryExt.CreateFileStorageAndFileManager(true,
            logger, localPath, sourceFileStorageName, fileStorages, null, null, cancellationToken);

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
            await FileManagersFactoryExt.CreateFileStorageAndFileManager(true, logger, localPath,
                destinationFileStorageName, fileStorages, null, null, cancellationToken);

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

        var localFileManager = FileManagersFactory.CreateFileManager(true, logger, localPath);

        if (localFileManager == null)
        {
            logger.LogError("localFileManager does not created");
            return null;
        }

        //პარამეტრების მიხედვით ბაზის სარეზერვო ასლის დამზადება და მოქაჩვა
        //წყაროს სერვერის აგენტის შექმნა
        var createDatabaseManagerResultForSource = await DatabaseManagersFactory.CreateDatabaseManager(logger, true,
            sourceDbConnectionName, databaseServerConnections, apiClients, httpClientFactory, null, null,
            cancellationToken);

        if (createDatabaseManagerResultForSource.IsT1)
        {
            Err.PrintErrorsOnConsole(createDatabaseManagerResultForSource.AsT1);
            logger.LogError("Can not create client for source Database server");
            return null;
        }

        var createDatabaseManagerResultForDestination = await DatabaseManagersFactory.CreateDatabaseManager(logger,
            true, destinationDbConnectionName, databaseServerConnections, apiClients, httpClientFactory, null, null,
            cancellationToken);

        if (createDatabaseManagerResultForDestination.IsT1)
        {
            Err.PrintErrorsOnConsole(createDatabaseManagerResultForDestination.AsT1);
            logger.LogError("Can not create client for destination Database server");
            return null;
        }

        var sourceDatabaseName = fromDatabaseParameters.DatabaseName;
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

        var needUploadToDestination = !FileStorageData.IsSameToLocal(destinationFileStorage, localPath) &&
                                      sourceFileStorageName != destinationFileStorageName;

        var needDownloadFromExchange = exchangeFileManager is not null && exchangeFileStorage is not null &&
                                       !FileStorageData.IsSameToLocal(exchangeFileStorage, localPath) &&
                                       exchangeFileStorageName != sourceFileStorageName;

        var exchangeSmartSchemaName = databasesBackupFilesExchangeParameters?.ExchangeSmartSchemaName;

        var exchangeSmartSchema = string.IsNullOrWhiteSpace(exchangeSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(exchangeSmartSchemaName);

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

        return new CopyBaseParameters(createSourceBaseBackupParametersResult.AsT0,
            createDestinationBaseBackupParametersResult.AsT0, exchangeFileManager, needUploadToDestination,
            needDownloadFromExchange, exchangeSmartSchema,
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.UploadTempExtension)
                ? "up!"
                : databasesBackupFilesExchangeParameters.UploadTempExtension);
    }
}