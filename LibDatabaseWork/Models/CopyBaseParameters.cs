using DatabasesManagement.Models;
using FileManagersMain;
using LibFileParameters.Models;
using LibParameters;

namespace LibDatabaseWork.Models;

public sealed class CopyBaseParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public CopyBaseParameters(BaseBackupParameters sourceBackupRestoreParameters,
        BaseBackupParameters destinationBackupRestoreParameters, FileManager? exchangeFileManager,
        bool needUploadToDestination, bool needDownloadFromExchange,
        SmartSchema? exchangeSmartSchema, string uploadTempExtension, string localPath)
    {
        SourceBackupParameters = sourceBackupRestoreParameters;
        DestinationBackupParameters = destinationBackupRestoreParameters;
        ExchangeFileManager = exchangeFileManager;
        //LocalFileManager = localFileManager;
        //NeedDownloadFromSource = needDownloadFromSource;
        //LocalSmartSchema = localSmartSchema;
        NeedUploadToDestination = needUploadToDestination;
        NeedDownloadFromExchange = needDownloadFromExchange;
        ExchangeSmartSchema = exchangeSmartSchema;
        //NeedDownloadFromDestination = needDownloadFromDestination;
        //NeedUploadDestinationToExchange = needUploadDestinationToExchange;
        //DownloadTempExtension = downloadTempExtension;
        UploadTempExtension = uploadTempExtension;
        LocalPath = localPath;
    }

    public BaseBackupParameters SourceBackupParameters { get; }
    public BaseBackupParameters DestinationBackupParameters { get; }
    public FileManager? ExchangeFileManager { get; }
    //public FileManager LocalFileManager { get; }

    //public bool NeedDownloadFromSource { get; }

    //public SmartSchema? LocalSmartSchema { get; }
    public bool NeedUploadToDestination { get; }
    public bool NeedDownloadFromExchange { get; }

    public SmartSchema? ExchangeSmartSchema { get; }
    //public bool NeedDownloadFromDestination { get; }

    //public bool NeedUploadDestinationToExchange { get; }

    //public string DownloadTempExtension { get; }
    public string UploadTempExtension { get; }
    public string LocalPath { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }
}