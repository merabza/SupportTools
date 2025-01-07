﻿using System;
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
    //bool fromProductionToDeveloper, 
    //, DatabasesParameters fromDatabaseParameters,
    // DatabasesParameters toDatabaseParameters
    //, string projectName
    //, ServerInfoModel serverInfo
    public static async Task<CopyBaseParameters?> CreateCopyBaseParameters(ILogger logger,
        IHttpClientFactory httpClientFactory, DatabasesParameters fromDatabaseParameters,
        DatabasesParameters toDatabaseParameters, SupportToolsParameters supportToolsParameters)
    {
        var databasesBackupFilesExchangeParameters = supportToolsParameters.DatabasesBackupFilesExchangeParameters;


        ////შევამოწმოთ პროექტის პარამეტრები
        //var project = supportToolsParameters.GetProject(projectName);
        //if (project == null)
        //{
        //    StShared.WriteErrorLine($"Project with name {projectName} not found", true);
        //    return null;
        //}

        //if (string.IsNullOrWhiteSpace(serverInfo.ServerName))
        //{
        //    StShared.WriteErrorLine("Server name is not specified", true);
        //    return null;
        //}

        //შევამოწმოთ სერვერის პარამეტრები
        //var server = supportToolsParameters.GetServerData(serverInfo.ServerName);
        //if (server is null)
        //{
        //    StShared.WriteErrorLine($"Server with name {serverInfo.ServerName} not found", true);
        //    return null;
        //}

        //var dep = serverInfo.DatabasesExchangeParameters;
        //if (dep is null)
        //{
        //    StShared.WriteErrorLine(
        //        $"DatabasesExchangeParameters is null for Server with name {serverInfo.GetItemKey()} for Project {projectName} not found",
        //        true);
        //    return null;
        //}

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
        //fromProductionToDeveloper
        //? dep.ProductionDbConnectionName
        //: dep.DeveloperDbConnectionName;

        var sourceDbWebAgentName = fromDatabaseParameters.DbWebAgentName;
        //fromProductionToDeveloper
        //? dep.ProductionDbWebAgentName
        //: dep.DeveloperDbWebAgentName;

        var backupBaseName = fromDatabaseParameters.DatabaseName;
        //fromProductionToDeveloper
        //? dep.CurrentProductionBaseName
        //: dep.DeveloperBaseName; //ბაზის სერვერის მხარე

        //მიზნის სერვერის აგენტის შექმნა
        var destinationDbWebAgentName = toDatabaseParameters.DbWebAgentName;
        //fromProductionToDeveloper
        //? dep.DeveloperDbWebAgentName
        //: dep.ProductionDbWebAgentName;

        var destinationDbConnectionName = toDatabaseParameters.DbConnectionName;
        //fromProductionToDeveloper
        //? dep.DeveloperDbConnectionName
        //: dep.ProductionDbConnectionName;

        var destinationBaseName = toDatabaseParameters.DatabaseName;
        //fromProductionToDeveloper
        //? dep.ProductionBaseCopyNameForDeveloperServer
        //: dep.NewProductionBaseName;

        //თუ წყარო და მიზანი ერთიდაიგივე არ არის ბაზების სახელები განსხვავებული უნდა იყოს.
        //ფაქტობრივად იმავე სერვერზე მოხდება ბაზის დაკოპირება
        if ((sourceDbConnectionName == destinationDbConnectionName ||
             sourceDbWebAgentName == destinationDbWebAgentName) && backupBaseName == destinationBaseName)
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
        //fromProductionToDeveloper
        //? dep.ProductionFileStorageName
        //: dep.DeveloperFileStorageName;

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
        //fromProductionToDeveloper ? dep.DeveloperFileStorageName : dep.ProductionFileStorageName;

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

        //პარამეტრების მიხედვით ბაზის სარეზერვო ასლის დამზადება და მოქაჩვა
        //წყაროს სერვერის აგენტის შექმნა
        var agentClientForSource = await DatabaseAgentClientsFabric.CreateDatabaseManager(true, logger,
            httpClientFactory, sourceDbWebAgentName, apiClients, sourceDbConnectionName, databaseServerConnections,
            null, null, CancellationToken.None);

        if (agentClientForSource is null)
        {
            logger.LogError("Can not create client for source Database server");
            return null;
        }

        var agentClientForDestination = await DatabaseAgentClientsFabric.CreateDatabaseManager(true, logger,
            httpClientFactory, destinationDbWebAgentName, apiClients, destinationDbConnectionName,
            databaseServerConnections, null, null, CancellationToken.None);

        if (agentClientForDestination is null)
        {
            logger.LogError("Can not create client for destination Database server");
            return null;
        }

        //var sourceDbBackupParameters = DatabaseBackupParametersDomain.Create(fromDatabaseParameters.);
        //    fromProductionToDeveloper ? dep.ProductionDbBackupParameters : dep.DeveloperDbBackupParameters,
        //    fromProductionToDeveloper ? dep.ProductionDbServerSideBackupPath : dep.DeveloperDbServerSideBackupPath);

        //if (sourceDbBackupParameters is null)
        //{
        //    logger.LogError("sourceDbBackupParameters does not created");
        //    return null;
        //}

        var sourceDatabaseName = fromDatabaseParameters.DatabaseName;
            //fromProductionToDeveloper ? dep.CurrentProductionBaseName : dep.DeveloperBaseName;
        if (string.IsNullOrWhiteSpace(sourceDatabaseName))
        {
            logger.LogError("sourceDatabaseName does not detected");
            return null;
        }

        //var destinationDbBackupParameters = DatabaseBackupParametersDomain.Create(
        //    fromProductionToDeveloper ? dep.DeveloperDbBackupParameters : dep.ProductionDbBackupParameters,
        //    fromProductionToDeveloper ? dep.DeveloperDbServerSideBackupPath : dep.ProductionDbServerSideBackupPath);

        //var destinationDbServerSideDataFolderPath = 
        //    fromProductionToDeveloper
        //    ? dep.DeveloperDbServerSideDataFolderPath
        //    : dep.ProductionDbServerSideDataFolderPath;

        //var destinationDbServerSideLogFolderPath = fromProductionToDeveloper
        //    ? dep.DeveloperDbServerSideLogFolderPath
        //    : dep.ProductionDbServerSideLogFolderPath;

        //if (destinationDbBackupParameters is null)
        //{
        //    logger.LogError("destinationDbBackupParameters does not created");
        //    return null;
        //}

        var destinationDatabaseName = toDatabaseParameters.DatabaseName;
        //fromProductionToDeveloper
        //    ? dep.ProductionBaseCopyNameForDeveloperServer
        //    : dep.NewProductionBaseName;

        if (string.IsNullOrWhiteSpace(destinationDatabaseName))
        {
            logger.LogError("destinationDatabaseName does not detected");
            return null;
        }

        var needDownloadFromSource = !FileStorageData.IsSameToLocal(sourceFileStorage, localPath);

        SmartSchemas smartSchemas = new(supportToolsParameters.SmartSchemas);

        var sourceSmartSchemaName = fromDatabaseParameters.SmartSchemaName;
            //fromProductionToDeveloper ? dep.ProductionSmartSchemaName : dep.DeveloperSmartSchemaName;

        var sourceSmartSchema = string.IsNullOrWhiteSpace(sourceSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(sourceSmartSchemaName);

        var localSmartSchemaName = databasesBackupFilesExchangeParameters?.LocalSmartSchemaName;

        var localSmartSchema = string.IsNullOrWhiteSpace(localSmartSchemaName)
            ? null
            : smartSchemas.GetSmartSchemaByKey(localSmartSchemaName);

        var needUploadToDestination = !FileStorageData.IsSameToLocal(destinationFileStorage, localPath) &&
                                      sourceFileStorageName != destinationFileStorageName;

        var destinationSmartSchemaName =toDatabaseParameters.SmartSchemaName;
        //fromProductionToDeveloper ? dep.DeveloperSmartSchemaName : dep.ProductionSmartSchemaName;

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

        return new CopyBaseParameters(agentClientForSource, agentClientForDestination, exchangeFileManager,
            sourceFileManager, destinationFileManager, localFileManager, 
            //sourceDbBackupParameters,
            //destinationDbBackupParameters, 
            needDownloadFromSource, sourceSmartSchema, localSmartSchema,
            needUploadToDestination, destinationSmartSchema, needDownloadFromExchange, exchangeSmartSchema,
            needDownloadFromDestination, needUploadDestinationToExchange,
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.DownloadTempExtension)
                ? "down!"
                : databasesBackupFilesExchangeParameters.DownloadTempExtension,
            string.IsNullOrWhiteSpace(databasesBackupFilesExchangeParameters?.UploadTempExtension)
                ? "up!"
                : databasesBackupFilesExchangeParameters.UploadTempExtension, sourceDatabaseName,
            //destinationDbServerSideDataFolderPath, destinationDbServerSideLogFolderPath, 
            destinationDatabaseName,
            localPath);
    }
}