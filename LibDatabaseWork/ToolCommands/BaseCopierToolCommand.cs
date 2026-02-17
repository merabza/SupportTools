using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.DatabasesManagement.Models;
using WebAgentContracts.WebAgentDatabasesApiContracts.V1.Responses;

namespace LibDatabaseWork.ToolCommands;

public sealed class BaseCopierToolCommand : ToolCommand
{
    private const string ActionName = "Copy Database";
    private const string ActionDescription = "Copy Database";
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BaseCopierToolCommand(ILogger logger, IParameters parameters, IParametersManager parametersManager) : base(
        logger, ActionName, parameters, parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private CopyBaseParameters CopyBaseParameters => (CopyBaseParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        BaseBackupParameters sourceBackupParameters = CopyBaseParameters.SourceBackupParameters;
        BaseBackupParameters destinationBackupParameters = CopyBaseParameters.DestinationBackupParameters;
        BackupRestoreParameters destinationBackupRestoreParameters =
            destinationBackupParameters.BackupRestoreParameters;

        _logger.LogInformation("Create Backup for source Database");

        var sourceBaseBackupRestorer = new BaseBackupRestoreTool(_logger, sourceBackupParameters);
        BackupFileParameters? backupFileParametersForSource =
            await sourceBaseBackupRestorer.CreateDatabaseBackup(cancellationToken);

        if (backupFileParametersForSource is null)
        {
            return false;
        }

        string fileName = backupFileParametersForSource.Name;
        string prefix = backupFileParametersForSource.Prefix;
        string suffix = backupFileParametersForSource.Suffix;
        string dateMask = backupFileParametersForSource.DateMask;

        //თუ მიზნის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს ან თუ წყაროს და მიზნის ფაილსაცავები ემთხვევა
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        if (CopyBaseParameters.NeedUploadToDestination)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Upload File {FileName} to Destination", fileName);
            }

            if (!destinationBackupRestoreParameters.FileManager.UploadFile(fileName,
                    CopyBaseParameters.UploadTempExtension))
            {
                _logger.LogError("Can not Upload File {FileName}", fileName);
                return false;
            }

            _logger.LogInformation("Remove Redundant Files for destination");

            destinationBackupRestoreParameters.FileManager.RemoveRedundantFiles(prefix, dateMask, suffix,
                destinationBackupRestoreParameters.SmartSchema);
        }

        //ან თუ გაცვლის ფაილსაცავი არ გვაქვს, ან ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        if (CopyBaseParameters is { NeedDownloadFromExchange: true, ExchangeFileManager: not null })
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Upload File {FileName} to Exchange", fileName);
            }

            if (!CopyBaseParameters.ExchangeFileManager.UploadFile(fileName, CopyBaseParameters.UploadTempExtension))
            {
                _logger.LogError("Can not Upload File {FileName}", fileName);
                return false;
            }

            _logger.LogInformation("Remove Redundant Files for exchange");

            CopyBaseParameters.ExchangeFileManager.RemoveRedundantFiles(prefix, dateMask, suffix,
                CopyBaseParameters.ExchangeSmartSchema);
        }

        var destinationBaseBackupRestorer = new BaseBackupRestoreTool(_logger, destinationBackupParameters);
        if (!destinationBackupParameters.SkipBackupBeforeRestore)
            //თუ SkipBackupBeforeRestore პარამეტრით აკრძალული არ არის
            //სანამ გადავაწერთ არსებული ბაზა გადავინახოთ ყოველი შემთხვევისათვის
        {
            await destinationBaseBackupRestorer.CreateDatabaseBackup(cancellationToken);
        }

        return await destinationBaseBackupRestorer.RestoreDatabaseFromBackup(backupFileParametersForSource,
            destinationBackupParameters.DatabaseRecoveryModel, cancellationToken);
    }
}
