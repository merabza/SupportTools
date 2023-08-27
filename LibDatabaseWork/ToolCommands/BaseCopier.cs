using System;
using CliParameters;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibDatabaseWork.ToolCommands;

public sealed class BaseCopier : ToolCommand
{
    private const string ActionName = "Copy Database";
    private const string ActionDescription = "Copy Database";

    public BaseCopier(ILogger logger, CopyBaseParameters parameters, IParametersManager parametersManager) : base(
        logger, ActionName, parameters, parametersManager, ActionDescription)
    {
    }

    private CopyBaseParameters CopyBaseParameters => (CopyBaseParameters)Par;

    private string GetActionDescription()
    {
        return
            $"Copy Database from {CopyBaseParameters.SourceDatabaseName} to {CopyBaseParameters.DestinationDatabaseName}";
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        Logger.LogInformation("Create Agent Client for source Database");

        var agentClientForSource = CopyBaseParameters.AgentClientForSource;

        Logger.LogInformation("Create Agent Client for destination Database");

        var agentClientForDestination = CopyBaseParameters.AgentClientForDestination;

        Console.Write("Create File storages: ");

        Logger.LogInformation("Create Backup for source Database");

        //ბექაპის დამზადება წყაროს მხარეს
        var backupFileParametersForSource = agentClientForSource
            .CreateBackup(CopyBaseParameters.SourceDbBackupParameters, CopyBaseParameters.SourceDatabaseName).Result;

        //თუ ბექაპის დამზადებისას რაიმე პრობლემა დაფისირდა, ვჩერდებით.
        if (backupFileParametersForSource == null)
        {
            Logger.LogError("Backup not created");
            return false;
        }

        //თუ წყაროს ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        if (!CopyBaseParameters.NeedDownloadFromSource)
        {
            Logger.LogInformation("Not need Download From Source");
        }

        else
        {
            Logger.LogInformation($"Download File {backupFileParametersForSource.Name}");

            //წყაროდან ლოკალურ ფოლდერში მოქაჩვა
            if (!CopyBaseParameters.SourceFileManager.DownloadFile(backupFileParametersForSource.Name,
                    CopyBaseParameters.DownloadTempExtension))
            {
                Logger.LogError($"Can not Download File {backupFileParametersForSource.Name}");
                return false;
            }

            Logger.LogInformation("Remove Redundant Files for source");

            CopyBaseParameters.SourceFileManager.RemoveRedundantFiles(backupFileParametersForSource.Prefix,
                backupFileParametersForSource.DateMask, backupFileParametersForSource.Suffix,
                CopyBaseParameters.SourceSmartSchema);
        }

        Logger.LogInformation("Remove Redundant Files for local");
        CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(backupFileParametersForSource.Prefix,
            backupFileParametersForSource.DateMask, backupFileParametersForSource.Suffix,
            CopyBaseParameters.LocalSmartSchema);

        //თუ მიზნის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს ან თუ წყაროს და მიზნის ფაილსაცავები ემთხვევა
        //   მაშინ მოქაჩვა საჭირო აღარ არის

        if (CopyBaseParameters.NeedUploadToDestination)
        {
            Logger.LogInformation($"Upload File {backupFileParametersForSource.Name} to Destination");

            if (!CopyBaseParameters.DestinationFileManager.UploadFile(backupFileParametersForSource.Name,
                    CopyBaseParameters.UploadTempExtension))
            {
                Logger.LogError($"Can not Upload File {backupFileParametersForSource.Name}");
                return false;
            }

            Logger.LogInformation("Remove Redundant Files for destination");

            CopyBaseParameters.DestinationFileManager.RemoveRedundantFiles(backupFileParametersForSource.Prefix,
                backupFileParametersForSource.DateMask, backupFileParametersForSource.Suffix,
                CopyBaseParameters.DestinationSmartSchema);
        }

        //ან თუ გაცვლის ფაილსაცავი არ გვაქვს, ან ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
        //   მაშინ მოქაჩვა საჭირო აღარ არის
        if (CopyBaseParameters.NeedDownloadFromExchange && CopyBaseParameters.ExchangeFileManager is not null)
        {
            Logger.LogInformation($"Upload File {backupFileParametersForSource.Name} to Exchange");

            if (!CopyBaseParameters.ExchangeFileManager.UploadFile(backupFileParametersForSource.Name,
                    CopyBaseParameters.UploadTempExtension))
            {
                Logger.LogError($"Can not Upload File {backupFileParametersForSource.Name}");
                return false;
            }

            Logger.LogInformation("Remove Redundant Files for exchange");


            CopyBaseParameters.ExchangeFileManager.RemoveRedundantFiles(backupFileParametersForSource.Prefix,
                backupFileParametersForSource.DateMask, backupFileParametersForSource.Suffix,
                CopyBaseParameters.ExchangeSmartSchema);
        }

        Logger.LogInformation($"Check if Destination base {CopyBaseParameters.DestinationDatabaseName} exists");

        //შევამოწმოთ მიზნის ბაზის არსებობა
        var result = agentClientForDestination.IsDatabaseExists(CopyBaseParameters.DestinationDatabaseName).Result;
        if (result.IsT1)
        {
            foreach (var err in result.AsT1) StShared.WriteErrorLine($"Error from server: {err.ErrorMessage}", true);
            return false;
        }

        if (result.AsT0)
        {
            Logger.LogInformation(
                $"Create Backup for Destination base {CopyBaseParameters.DestinationDatabaseName}");

            var backupFileParametersForDestination = agentClientForDestination
                .CreateBackup(CopyBaseParameters.DestinationDbBackupParameters,
                    CopyBaseParameters.DestinationDatabaseName).Result;

            if (backupFileParametersForDestination == null)
            {
                Logger.LogError($"{GetActionDescription()} - finished with errors");
                return false;
            }

            Logger.LogInformation("Remove Redundant Files for destination");

            CopyBaseParameters.DestinationFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                CopyBaseParameters.DestinationSmartSchema);

            //თუ მიზნის ფაილსაცავი ლოკალურია და მისი ფოლდერი ემთხვევა პარამეტრების ლოკალურ ფოლდერს.
            //   მაშინ მოქაჩვა საჭირო აღარ არის

            if (CopyBaseParameters.NeedDownloadFromDestination)
            {
                Logger.LogInformation($"Download File {backupFileParametersForDestination.Name} from Destination");

                if (!CopyBaseParameters.DestinationFileManager.DownloadFile(backupFileParametersForDestination.Name,
                        CopyBaseParameters.DownloadTempExtension))
                {
                    Logger.LogError($"Can not Download File {backupFileParametersForDestination.Name}");
                    return false;
                }

                Logger.LogInformation("Remove Redundant Files for local");
                CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                    backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                    CopyBaseParameters.LocalSmartSchema);
            }

            if (CopyBaseParameters.NeedUploadDestinationToExchange &&
                CopyBaseParameters.ExchangeFileManager is not null)
            {
                Logger.LogInformation($"Upload File {backupFileParametersForDestination.Name} to Exchange");

                if (!CopyBaseParameters.ExchangeFileManager.UploadFile(backupFileParametersForDestination.Name,
                        CopyBaseParameters.UploadTempExtension))
                {
                    Logger.LogError($"Can not Upload File {backupFileParametersForDestination.Name}");
                    return false;
                }

                Logger.LogInformation("Remove Redundant Files for local");
                CopyBaseParameters.LocalFileManager.RemoveRedundantFiles(backupFileParametersForDestination.Prefix,
                    backupFileParametersForDestination.DateMask, backupFileParametersForDestination.Suffix,
                    CopyBaseParameters.ExchangeSmartSchema);
            }
        }

        //მიზნის ბაზის აღდგენა აქაჩული ბექაპის გამოყენებით
        Logger.LogInformation($"Restoring database {CopyBaseParameters.DestinationDatabaseName}");

        var success = agentClientForDestination
            .RestoreDatabaseFromBackup(backupFileParametersForSource, CopyBaseParameters.DestinationDatabaseName,
                CopyBaseParameters.LocalPath)
            .Result;

        if (success)
            return true;

        Logger.LogError("something wrong");
        return false;
    }
}