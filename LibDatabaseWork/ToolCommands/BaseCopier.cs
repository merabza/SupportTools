using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared.Errors;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibDatabaseWork.ToolCommands;

public sealed class BaseCopier : ToolCommand
{
    private const string ActionName = "Copy Database";
    private const string ActionDescription = "Copy Database";
    private readonly ILogger _logger;

    public BaseCopier(ILogger logger, IParameters parameters, IParametersManager parametersManager) : base(logger,
        ActionName, parameters, parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private CopyBaseParameters CopyBaseParameters => (CopyBaseParameters)Par;

    private string GetActionDescription()
    {
        return
            $"Copy Database from {CopyBaseParameters.SourceBackupRestoreParameters.DatabaseName} to {CopyBaseParameters.DestinationBackupRestoreParameters.DatabaseName}";
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var sourceBackupRestoreParameters = CopyBaseParameters.SourceBackupRestoreParameters;
        var destinationBackupRestoreParameters = CopyBaseParameters.DestinationBackupRestoreParameters;

        var databaseManagerForSource = sourceBackupRestoreParameters.DatabaseManager;
        var databaseManagerForDestination = destinationBackupRestoreParameters.DatabaseManager;

        _logger.LogInformation("Create Backup for source Database");

        //ბექაპის დამზადება წყაროს მხარეს
        var createBackupResult = await databaseManagerForSource.CreateBackup(sourceBackupRestoreParameters.DatabaseName,
            sourceBackupRestoreParameters.DbServerFoldersSetName, CancellationToken.None);

        //თუ ბექაპის დამზადებისას რაიმე პრობლემა დაფიქსირდა, ვჩერდებით.
        if (createBackupResult.IsT1)
        {
            _logger.LogError("Backup not created");
            return false;
        }

        var backupFileParametersForSource = createBackupResult.AsT0;

        var fileName = backupFileParametersForSource.Name;
        var prefix = backupFileParametersForSource.Prefix;
        var suffix = backupFileParametersForSource.Suffix;
        //თუ წყაროს ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        if (!CopyBaseParameters.NeedDownloadFromSource)
        {
            _logger.LogInformation("Not need Download From Source");
        }
        else
        {
            _logger.LogInformation("Download File {fileName}", fileName);

            //წყაროდან ლოკალურ ფოლდერში მოქაჩვა
            if (!sourceBackupRestoreParameters.FileManager.DownloadFile(fileName,
                    CopyBaseParameters.DownloadTempExtension))
            {
                _logger.LogError("Can not Download File {fileName}", fileName);
                return false;
            }

            _logger.LogInformation("Remove Redundant Files for source");

            sourceBackupRestoreParameters.FileManager.RemoveRedundantFiles(prefix,
                backupFileParametersForSource.DateMask, suffix, sourceBackupRestoreParameters.SmartSchema);
        }

        _logger.LogInformation("Remove Redundant Files for local");
        CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(prefix, backupFileParametersForSource.DateMask, suffix,
            CopyBaseParameters.LocalSmartSchema);

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

            destinationBackupRestoreParameters.FileManager.RemoveRedundantFiles(prefix,
                backupFileParametersForSource.DateMask, suffix, destinationBackupRestoreParameters.SmartSchema);
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


            CopyBaseParameters.ExchangeFileManager.RemoveRedundantFiles(prefix, backupFileParametersForSource.DateMask,
                suffix, CopyBaseParameters.ExchangeSmartSchema);
        }

        var destinationDatabaseName = destinationBackupRestoreParameters.DatabaseName;
        _logger.LogInformation("Check if Destination base {destinationDatabaseName} exists", destinationDatabaseName);

        //შევამოწმოთ მიზნის ბაზის არსებობა
        var isDatabaseExistsResult =
            await databaseManagerForDestination.IsDatabaseExists(destinationDatabaseName, CancellationToken.None);

        if (isDatabaseExistsResult.IsT1)
        {
            _logger.LogInformation("The existence of the base could not be determined");
            return false;
        }

        var isDatabaseExists = isDatabaseExistsResult.AsT0;

        if (isDatabaseExists)
        {
            _logger.LogInformation("Create Backup for Destination base {destinationDatabaseName}",
                destinationDatabaseName);

            var createBackupResult2 = await databaseManagerForDestination.CreateBackup(destinationDatabaseName,
                destinationBackupRestoreParameters.DbServerFoldersSetName, CancellationToken.None);

            if (createBackupResult2.IsT1)
            {
                var actionDescription = GetActionDescription();
                _logger.LogError("{actionDescription} - finished with errors", actionDescription);
                return false;
            }

            var backupFileParametersForDestination = createBackupResult2.AsT0;

            _logger.LogInformation("Remove Redundant Files for destination");

            destinationBackupRestoreParameters.FileManager.RemoveRedundantFiles(
                backupFileParametersForDestination.Prefix, backupFileParametersForDestination.DateMask,
                backupFileParametersForDestination.Suffix, destinationBackupRestoreParameters.SmartSchema);

            //თუ მიზნის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
            //   მაშინ მოქაჩვა საჭირო აღარ არის

            var destinationFileName = backupFileParametersForDestination.Name;
            if (CopyBaseParameters.NeedDownloadFromDestination)
            {
                _logger.LogInformation("Download File {destinationFileName} from Destination", destinationFileName);

                if (!destinationBackupRestoreParameters.FileManager.DownloadFile(destinationFileName,
                        CopyBaseParameters.DownloadTempExtension))
                {
                    _logger.LogError("Can not Download File {destinationFileName}", destinationFileName);
                    return false;
                }

                _logger.LogInformation("Remove Redundant Files for local");
                CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                    backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                    CopyBaseParameters.LocalSmartSchema);
            }

            if (CopyBaseParameters is { NeedUploadDestinationToExchange: true, ExchangeFileManager: not null })
            {
                _logger.LogInformation("Upload File {destinationFileName} to Exchange", destinationFileName);

                if (!CopyBaseParameters.ExchangeFileManager.UploadFile(destinationFileName,
                        CopyBaseParameters.UploadTempExtension))
                {
                    _logger.LogError("Can not Upload File {destinationFileName}", destinationFileName);
                    return false;
                }

                _logger.LogInformation("Remove Redundant Files for local");
                CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                    backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                    CopyBaseParameters.ExchangeSmartSchema);
            }
        }

        //მიზნის ბაზის აღდგენა აქაჩული ბექაპის გამოყენებით
        _logger.LogInformation("Restoring database {destinationDatabaseName}", destinationDatabaseName);

        var restoreDatabaseFromBackupResult = await databaseManagerForDestination.RestoreDatabaseFromBackup(
            backupFileParametersForSource, destinationDatabaseName,
            destinationBackupRestoreParameters.DbServerFoldersSetName, CopyBaseParameters.LocalPath,
            CancellationToken.None);

        if (restoreDatabaseFromBackupResult.IsNone)
            return true;

        Err.PrintErrorsOnConsole((Err[])restoreDatabaseFromBackupResult);
        _logger.LogError("something went wrong");
        return false;
    }
}