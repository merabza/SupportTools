using ParametersManagement.LibParameters;
using ToolsManagement.DatabasesManagement.Models;
using ToolsManagement.FileManagersMain;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace LibDatabaseWork.Models;

public sealed class RestoreFromFileStorageParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public RestoreFromFileStorageParameters(BaseBackupParameters destinationBackupParameters,
        FileManager sourceFileManager, BackupFileParameters backupFile, bool needDownloadFromSource,
        bool needUploadToDestination, bool needUploadToExchange)
    {
        DestinationBackupParameters = destinationBackupParameters;
        SourceFileManager = sourceFileManager;
        BackupFile = backupFile;
        NeedDownloadFromSource = needDownloadFromSource;
        NeedUploadToDestination = needUploadToDestination;
        NeedUploadToExchange = needUploadToExchange;
    }

    public BaseBackupParameters DestinationBackupParameters { get; }
    public FileManager SourceFileManager { get; }
    public BackupFileParameters BackupFile { get; }
    public bool NeedDownloadFromSource { get; }
    public bool NeedUploadToDestination { get; }
    public bool NeedUploadToExchange { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }
}
