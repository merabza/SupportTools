using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using DatabasesManagement;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.ToolCommands;

public sealed class BaseCopier : ToolCommand
{
    private const string ActionName = "Copy Database";
    private const string ActionDescription = "Copy Database";
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BaseCopier(ILogger logger, IParameters parameters, IParametersManager parametersManager) : base(logger,
        ActionName, parameters, parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private CopyBaseParameters CopyBaseParameters => (CopyBaseParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var sourceBackupParameters = CopyBaseParameters.SourceBackupParameters;
        var destinationBackupParameters = CopyBaseParameters.DestinationBackupParameters;
        var destinationBackupRestoreParameters = destinationBackupParameters.BackupRestoreParameters;

        _logger.LogInformation("Create Backup for source Database");

        var sourceBaseBackupRestorer = new BaseBackupRestorer(_logger, sourceBackupParameters);
        var backupFileParametersForSource = await sourceBaseBackupRestorer.CreateDatabaseBackup(cancellationToken);

        if (backupFileParametersForSource is null)
            return false;

        var fileName = backupFileParametersForSource.Name;
        var prefix = backupFileParametersForSource.Prefix;
        var suffix = backupFileParametersForSource.Suffix;
        var dateMask = backupFileParametersForSource.DateMask;

        //თუ მიზნის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს ან თუ წყაროს და მიზნის ფაილსაცავები ემთხვევა
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        if (CopyBaseParameters.NeedUploadToDestination)
        {
            _logger.LogInformation("Upload File {fileName} to Destination", fileName);

            if (!destinationBackupRestoreParameters.FileManager.UploadFile(fileName,
                    CopyBaseParameters.UploadTempExtension))
            {
                _logger.LogError("Can not Upload File {fileName}", fileName);
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
            _logger.LogInformation("Upload File {fileName} to Exchange", fileName);

            if (!CopyBaseParameters.ExchangeFileManager.UploadFile(fileName, CopyBaseParameters.UploadTempExtension))
            {
                _logger.LogError("Can not Upload File {fileName}", fileName);
                return false;
            }

            _logger.LogInformation("Remove Redundant Files for exchange");

            CopyBaseParameters.ExchangeFileManager.RemoveRedundantFiles(prefix, dateMask, suffix,
                CopyBaseParameters.ExchangeSmartSchema);
        }

        var destinationBaseBackupRestorer = new BaseBackupRestorer(_logger, destinationBackupParameters);
        if (!destinationBackupParameters.SkipBackupBeforeRestore)
            //თუ SkipBackupBeforeRestore პარამეტრით აკრძალული არ არის
            //სანამ გადავაწერთ არსებული ბაზა გადავინახოთ ყოველი შემთხვევისათვის
            await destinationBaseBackupRestorer.CreateDatabaseBackup(cancellationToken);

        return await destinationBaseBackupRestorer.RestoreDatabaseFromBackup(backupFileParametersForSource,
            cancellationToken);
    }
}