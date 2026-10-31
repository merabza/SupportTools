using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.DatabasesManagement.Models;

namespace LibDatabaseWork.ToolCommands.RestoreDatabaseFromFileStorageToNewDatabase;

public sealed class RestoreDatabaseFromFileStorageToNewDatabaseToolCommand : ToolCommand
{
    private const string ActionName = "Restore Database From File Storage";
    private const string ActionDescription = "Restore Database From File Storage to New Database";
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RestoreDatabaseFromFileStorageToNewDatabaseToolCommand(ILogger logger, IParameters parameters,
        IParametersManager parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
    }

    private RestoreFromFileStorageParameters RestoreParameters => (RestoreFromFileStorageParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        BaseBackupParameters destinationBackupParameters = RestoreParameters.DestinationBackupParameters;
        string fileName = RestoreParameters.BackupFile.Name;

        //არჩეული ფაილის ფაილსაცავიდან ლოკალურ ფოლდერში მოქაჩვა
        if (RestoreParameters.NeedDownloadFromSource)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Download File {FileName} from File Storage", fileName);
            }

            if (!RestoreParameters.SourceFileManager.DownloadFile(fileName,
                    destinationBackupParameters.DownloadTempExtension))
            {
                _logger.LogError("Can not Download File {FileName}", fileName);
                return false;
            }
        }

        //ფაილის ატვირთვა მიზნის სერვერის ფაილსაცავში
        if (RestoreParameters.NeedUploadToDestination)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Upload File {FileName} to Destination", fileName);
            }

            if (!destinationBackupParameters.BackupRestoreParameters.FileManager.UploadFile(fileName,
                    destinationBackupParameters.UploadTempExtension))
            {
                _logger.LogError("Can not Upload File {FileName}", fileName);
                return false;
            }
        }

        //ფაილის ატვირთვა გაცვლის ფაილსაცავში, საიდანაც მას მოშორებული სერვერის აგენტი მოქაჩავს
        if (RestoreParameters.NeedUploadToExchange &&
            destinationBackupParameters.ExchangeFileManager is { } exchangeFileManager)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Upload File {FileName} to Exchange", fileName);
            }

            if (!exchangeFileManager.UploadFile(fileName, destinationBackupParameters.UploadTempExtension))
            {
                _logger.LogError("Can not Upload File {FileName}", fileName);
                return false;
            }
        }

        var destinationBaseBackupRestorer = new BaseBackupRestoreTool(_logger, destinationBackupParameters);
        if (!destinationBackupParameters.SkipBackupBeforeRestore)
            //თუ SkipBackupBeforeRestore პარამეტრით აკრძალული არ არის
            //სანამ გადავაწერთ არსებული ბაზა გადავინახოთ ყოველი შემთხვევისათვის
        {
            await destinationBaseBackupRestorer.CreateDatabaseBackup(cancellationToken);
        }

        return await destinationBaseBackupRestorer.RestoreDatabaseFromBackup(RestoreParameters.BackupFile,
            destinationBackupParameters.DatabaseRecoveryModel, cancellationToken);
    }
}
