using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using ToolsManagement.DatabasesManagement.Models;
using ToolsManagement.FileManagersMain;

namespace LibDatabaseWork.Models;

public sealed class CopyBaseParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public CopyBaseParameters(BaseBackupParameters sourceBackupRestoreParameters,
        BaseBackupParameters destinationBackupRestoreParameters, FileManager? exchangeFileManager,
        bool needUploadToDestination, bool needDownloadFromExchange, SmartSchema? exchangeSmartSchema,
        string uploadTempExtension)
    {
        SourceBackupParameters = sourceBackupRestoreParameters;
        DestinationBackupParameters = destinationBackupRestoreParameters;
        ExchangeFileManager = exchangeFileManager;
        NeedUploadToDestination = needUploadToDestination;
        NeedDownloadFromExchange = needDownloadFromExchange;
        ExchangeSmartSchema = exchangeSmartSchema;
        UploadTempExtension = uploadTempExtension;
    }

    public BaseBackupParameters SourceBackupParameters { get; }
    public BaseBackupParameters DestinationBackupParameters { get; }
    public FileManager? ExchangeFileManager { get; }
    public bool NeedUploadToDestination { get; }
    public bool NeedDownloadFromExchange { get; }
    public SmartSchema? ExchangeSmartSchema { get; }
    public string UploadTempExtension { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }
}
