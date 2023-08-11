using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceUpdater : ToolCommand
{
    private const string ActionName = "Update App";
    private const string ActionDescription = "Update App";

    public ServiceUpdater(ILogger logger, bool useConsole, ServiceUpdaterParameters programServiceUpdaterParameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, programServiceUpdaterParameters,
        parametersManager, ActionDescription)
    {
    }

    private ServiceUpdaterParameters ProgramServiceUpdaterParameters => (ServiceUpdaterParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var projectName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ProjectName;
        var environmentName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ServerInfo.EnvironmentName;

        if (string.IsNullOrEmpty(environmentName))
        {
            Logger.LogError("Environment {environmentName} is empty", environmentName);
            return false;
        }

        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var programPublisherParameters =
            ProgramServiceUpdaterParameters.ProgramPublisherParameters;

        var createPackageAndUpload = new CreatePackageAndUpload(Logger, UseConsole,
            programPublisherParameters.ProjectName, programPublisherParameters.MainProjectFileName,
            programPublisherParameters.ServerInfo, programPublisherParameters.WorkFolder,
            programPublisherParameters.DateMask, programPublisherParameters.Runtime,
            programPublisherParameters.RedundantFileNames, programPublisherParameters.UploadTempExtension,
            programPublisherParameters.FileStorageForExchange, programPublisherParameters.SmartSchemaForLocal,
            programPublisherParameters.SmartSchemaForExchange);

        if (!createPackageAndUpload.Run())
            return false;


        //2. დავშიფროთ პარამეტრების ფაილი და ავტვირთოთ ფაილსაცავში
        var appSettingsEncoderParameters =
            ProgramServiceUpdaterParameters.AppSettingsEncoderParameters;
        string? appSettingsVersion = null;
        var installParameters = !string.IsNullOrWhiteSpace(appSettingsEncoderParameters.AppSettingsJsonSourceFileName);
        if (installParameters)
        {
            var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(Logger, UseConsole,
                appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
                appSettingsEncoderParameters.AppSettingsJsonSourceFileName,
                appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, appSettingsEncoderParameters.KeyPart1,
                appSettingsEncoderParameters.KeyPart2, appSettingsEncoderParameters.ProjectName,
                appSettingsEncoderParameters.ServerInfo, appSettingsEncoderParameters.DateMask,
                appSettingsEncoderParameters.ParametersFileExtension,
                appSettingsEncoderParameters.FileStorageForExchange, appSettingsEncoderParameters.ExchangeSmartSchema);
            if (!encodeParametersAndUploadAction.Run() && ProgramServiceUpdaterParameters.IsService)
                return false;
            appSettingsVersion = encodeParametersAndUploadAction.AppSettingsVersion;
        }

        //3. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
        var installProgramAction = new InstallServiceAction(Logger, UseConsole,
            ProgramServiceUpdaterParameters.InstallerBaseParameters,
            ProgramServiceUpdaterParameters.ProgramArchiveDateMask,
            ProgramServiceUpdaterParameters.ProgramArchiveExtension,
            ProgramServiceUpdaterParameters.ParametersFileDateMask,
            ProgramServiceUpdaterParameters.ParametersFileExtension,
            ProgramServiceUpdaterParameters.FileStorageForDownload, projectName, environmentName,
            ProgramServiceUpdaterParameters.ServiceName, ProgramServiceUpdaterParameters.ServiceUserName,
            appSettingsEncoderParameters.AppSettingsEncodedJsonFileName);
        if (!installProgramAction.Run())
        {
            Logger.LogError("project {projectName}/{environmentName} was not updated", projectName, environmentName);
            return false;
        }

        //string installingProgramVersion = installProgramAction.InstallingProgramVersion;
        //4. შევამოწმოთ, რომ გაშვებული პროგრამის ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით//, projectName
        var checkProgramVersionAction = new CheckProgramVersionAction(Logger, UseConsole,
            ProgramServiceUpdaterParameters.CheckVersionParameters.WebAgentForCheck,
            ProgramServiceUpdaterParameters.ProxySettings, createPackageAndUpload.AssemblyVersion);

        if (!checkProgramVersionAction.Run())
        {
            Logger.LogError("project {projectName}/{environmentName} version check failed", projectName,
                environmentName);
            return false;
        }

        if (installParameters)
        {
            //5. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
            //, projectName
            var checkParametersVersionAction = new CheckParametersVersionAction(Logger, UseConsole,
                ProgramServiceUpdaterParameters.CheckVersionParameters.WebAgentForCheck,
                ProgramServiceUpdaterParameters.ProxySettings, appSettingsVersion);

            if (checkParametersVersionAction.Run())
                return true;
        }

        Logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
            environmentName);
        return false;
    }
}