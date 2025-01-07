using DatabasesManagement;
using FileManagersMain;
using LibFileParameters.Models;
using LibParameters;

namespace LibDatabaseWork.Models;

public sealed class CopyBaseParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public CopyBaseParameters(IDatabaseManager databaseManagerForSource, IDatabaseManager databaseManagerForDestination,
        FileManager? exchangeFileManager, FileManager sourceFileManager, FileManager destinationFileManager,
        FileManager localFileManager,
        //DatabaseBackupParametersDomain sourceDbBackupParameters,
        //DatabaseBackupParametersDomain destinationDbBackupParameters, 
        bool needDownloadFromSource, SmartSchema? sourceSmartSchema, SmartSchema? localSmartSchema,
        bool needUploadToDestination, SmartSchema? destinationSmartSchema, bool needDownloadFromExchange,
        SmartSchema? exchangeSmartSchema, bool needDownloadFromDestination, bool needUploadDestinationToExchange,
        string downloadTempExtension, string uploadTempExtension, string sourceDatabaseName,
        //string? destinationDbServerSideDataFolderPath,
        //string? destinationDbServerSideLogFolderPath, 
        string destinationDatabaseName, string localPath)
    {
        DatabaseManagerForSource = databaseManagerForSource;
        DatabaseManagerForDestination = databaseManagerForDestination;
        ExchangeFileManager = exchangeFileManager;
        SourceFileManager = sourceFileManager;
        DestinationFileManager = destinationFileManager;
        LocalFileManager = localFileManager;
        //SourceDbBackupParameters = sourceDbBackupParameters;
        //DestinationDbBackupParameters = destinationDbBackupParameters;
        NeedDownloadFromSource = needDownloadFromSource;
        SourceSmartSchema = sourceSmartSchema;
        LocalSmartSchema = localSmartSchema;
        NeedUploadToDestination = needUploadToDestination;
        DestinationSmartSchema = destinationSmartSchema;
        NeedDownloadFromExchange = needDownloadFromExchange;
        ExchangeSmartSchema = exchangeSmartSchema;
        NeedDownloadFromDestination = needDownloadFromDestination;
        NeedUploadDestinationToExchange = needUploadDestinationToExchange;
        DownloadTempExtension = downloadTempExtension;
        UploadTempExtension = uploadTempExtension;
        SourceDatabaseName = sourceDatabaseName;
        //DestinationDbServerSideDataFolderPath = destinationDbServerSideDataFolderPath;
        //DestinationDbServerSideLogFolderPath = destinationDbServerSideLogFolderPath;
        DestinationDatabaseName = destinationDatabaseName;
        LocalPath = localPath;
    }

    public IDatabaseManager DatabaseManagerForSource { get; }
    public IDatabaseManager DatabaseManagerForDestination { get; }
    public FileManager? ExchangeFileManager { get; }
    public FileManager SourceFileManager { get; }
    public FileManager DestinationFileManager { get; }

    public FileManager LocalFileManager { get; }

    //public DatabaseBackupParametersDomain SourceDbBackupParameters { get; }
    public string SourceDatabaseName { get; }

    //public string? DestinationDbServerSideDataFolderPath { get; }
    //public string? DestinationDbServerSideLogFolderPath { get; }
    //public DatabaseBackupParametersDomain DestinationDbBackupParameters { get; }
    public string DestinationDatabaseName { get; }
    public bool NeedDownloadFromSource { get; }
    public SmartSchema? SourceSmartSchema { get; }
    public SmartSchema? LocalSmartSchema { get; }
    public bool NeedUploadToDestination { get; }
    public SmartSchema? DestinationSmartSchema { get; }
    public bool NeedDownloadFromExchange { get; }
    public SmartSchema? ExchangeSmartSchema { get; }
    public bool NeedDownloadFromDestination { get; }
    public bool NeedUploadDestinationToExchange { get; }
    public string DownloadTempExtension { get; }
    public string UploadTempExtension { get; }
    public string LocalPath { get; }


    public bool CheckBeforeSave()
    {
        return true;
    }
}