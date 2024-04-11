using System;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibDatabaseWork.ToolCommands;

public sealed class BaseCopier : ToolCommand
{
    private const string ActionName = "Copy Database";
    private const string ActionDescription = "Copy Database";

    public BaseCopier(ILogger logger, IParameters parameters, IParametersManager parametersManager) : base(
        logger, ActionName, parameters, parametersManager, ActionDescription)
    {
    }

    private CopyBaseParameters CopyBaseParameters => (CopyBaseParameters)Par;

    private string GetActionDescription()
    {
        return
            $"Copy Database from {CopyBaseParameters.SourceDatabaseName} to {CopyBaseParameters.DestinationDatabaseName}";
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Create Agent Client for source Database");

        var agentClientForSource = CopyBaseParameters.AgentClientForSource;

        Logger.LogInformation("Create Agent Client for destination Database");

        var agentClientForDestination = CopyBaseParameters.AgentClientForDestination;

        Console.Write("Create File storages: ");

        Logger.LogInformation("Create Backup for source Database");

        //ბექაპის დამზადება წყაროს მხარეს
        var createBackupResult = await agentClientForSource.CreateBackup(CopyBaseParameters.SourceDbBackupParameters,
            CopyBaseParameters.SourceDatabaseName, CancellationToken.None);

        //თუ ბექაპის დამზადებისას რაიმე პრობლემა დაფიქსირდა, ვჩერდებით.
        if (createBackupResult.IsT1)
        {
            Logger.LogError("Backup not created");
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
            Logger.LogInformation("Not need Download From Source");
        }

        else
        {
            Logger.LogInformation("Download File {fileName}", fileName);

            //წყაროდან ლოკალურ ფოლდერში მოქაჩვა
            if (!CopyBaseParameters.SourceFileManager.DownloadFile(fileName,
                    CopyBaseParameters.DownloadTempExtension))
            {
                Logger.LogError("Can not Download File {fileName}", fileName);
                return false;
            }

            Logger.LogInformation("Remove Redundant Files for source");

            CopyBaseParameters.SourceFileManager.RemoveRedundantFiles(prefix,
                backupFileParametersForSource.DateMask, suffix,
                CopyBaseParameters.SourceSmartSchema);
        }

        Logger.LogInformation("Remove Redundant Files for local");
        CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(prefix,
            backupFileParametersForSource.DateMask, suffix,
            CopyBaseParameters.LocalSmartSchema);

        //თუ მიზნის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს ან თუ წყაროს და მიზნის ფაილსაცავები ემთხვევა
        //   მაშინ მოქაჩვა საჭირო აღარ არის

        if (CopyBaseParameters.NeedUploadToDestination)
        {
            Logger.LogInformation("Upload File {fileName} to Destination", fileName);

            if (!CopyBaseParameters.DestinationFileManager.UploadFile(fileName,
                    CopyBaseParameters.UploadTempExtension))
            {
                Logger.LogError("Can not Upload File {fileName}", fileName);
                return false;
            }

            Logger.LogInformation("Remove Redundant Files for destination");

            CopyBaseParameters.DestinationFileManager.RemoveRedundantFiles(prefix,
                backupFileParametersForSource.DateMask, suffix,
                CopyBaseParameters.DestinationSmartSchema);
        }

        //ან თუ გაცვლის ფაილსაცავი არ გვაქვს, ან ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        if (CopyBaseParameters is { NeedDownloadFromExchange: true, ExchangeFileManager: not null })
        {
            Logger.LogInformation("Upload File {fileName} to Exchange", fileName);

            if (!CopyBaseParameters.ExchangeFileManager.UploadFile(fileName, CopyBaseParameters.UploadTempExtension))
            {
                Logger.LogError("Can not Upload File {fileName}", fileName);
                return false;
            }

            Logger.LogInformation("Remove Redundant Files for exchange");


            CopyBaseParameters.ExchangeFileManager.RemoveRedundantFiles(prefix,
                backupFileParametersForSource.DateMask, suffix,
                CopyBaseParameters.ExchangeSmartSchema);
        }

        var destinationDatabaseName = CopyBaseParameters.DestinationDatabaseName;
        Logger.LogInformation("Check if Destination base {destinationDatabaseName} exists", destinationDatabaseName);

        //შევამოწმოთ მიზნის ბაზის არსებობა
        var isDatabaseExistsResult = await agentClientForDestination
            .IsDatabaseExists(destinationDatabaseName, CancellationToken.None);

        if (isDatabaseExistsResult.IsT1)
        {
            Logger.LogInformation("The existence of the base could not be determined");
            return false;
        }

        var isDatabaseExists = isDatabaseExistsResult.AsT0;

        if (isDatabaseExists)
        {
            Logger.LogInformation("Create Backup for Destination base {destinationDatabaseName}",
                destinationDatabaseName);

            var createBackupResult2 = await agentClientForDestination.CreateBackup(
                CopyBaseParameters.DestinationDbBackupParameters, destinationDatabaseName, CancellationToken.None);

            if (createBackupResult2.IsT1)
            {
                var actionDescription = GetActionDescription();
                Logger.LogError("{actionDescription} - finished with errors", actionDescription);
                return false;
            }

            var backupFileParametersForDestination = createBackupResult2.AsT0;

            Logger.LogInformation("Remove Redundant Files for destination");

            CopyBaseParameters.DestinationFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                CopyBaseParameters.DestinationSmartSchema);

            //თუ მიზნის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
            //   მაშინ მოქაჩვა საჭირო აღარ არის

            var destinationFileName = backupFileParametersForDestination.Name;
            if (CopyBaseParameters.NeedDownloadFromDestination)
            {
                Logger.LogInformation("Download File {destinationFileName} from Destination", destinationFileName);

                if (!CopyBaseParameters.DestinationFileManager.DownloadFile(destinationFileName,
                        CopyBaseParameters.DownloadTempExtension))
                {
                    Logger.LogError("Can not Download File {destinationFileName}", destinationFileName);
                    return false;
                }

                Logger.LogInformation("Remove Redundant Files for local");
                CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                    backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                    CopyBaseParameters.LocalSmartSchema);
            }

            if (CopyBaseParameters is { NeedUploadDestinationToExchange: true, ExchangeFileManager: not null })
            {
                Logger.LogInformation("Upload File {destinationFileName} to Exchange", destinationFileName);

                if (!CopyBaseParameters.ExchangeFileManager.UploadFile(destinationFileName,
                        CopyBaseParameters.UploadTempExtension))
                {
                    Logger.LogError("Can not Upload File {destinationFileName}", destinationFileName);
                    return false;
                }

                Logger.LogInformation("Remove Redundant Files for local");
                CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                    backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                    CopyBaseParameters.ExchangeSmartSchema);
            }
        }

        //მიზნის ბაზის აღდგენა აქაჩული ბექაპის გამოყენებით
        Logger.LogInformation("Restoring database {destinationDatabaseName}", destinationDatabaseName);

        var restoreDatabaseFromBackupResult = await agentClientForDestination.RestoreDatabaseFromBackup(
            backupFileParametersForSource,
            destinationDatabaseName, CancellationToken.None, CopyBaseParameters.LocalPath);

        if (restoreDatabaseFromBackupResult.IsNone)
            return true;

        Err.PrintErrorsOnConsole((Err[])restoreDatabaseFromBackupResult);
        Logger.LogError("something went wrong");
        return false;
    }
}