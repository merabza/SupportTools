using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceUpdater : ToolCommand
{
    private readonly ILogger _logger;
    private const string ActionName = "Update App";
    private const string ActionDescription = "Update App";

    public ServiceUpdater(ILogger logger, ServiceUpdaterParameters programServiceUpdaterParameters,
        IParametersManager parametersManager) : base(logger, ActionName, programServiceUpdaterParameters,
        parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private ServiceUpdaterParameters ProgramServiceUpdaterParameters => (ServiceUpdaterParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var projectName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ProjectName;
        var environmentName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ServerInfo.EnvironmentName;

        if (string.IsNullOrEmpty(environmentName))
        {
            _logger.LogError("Environment {environmentName} is empty", environmentName);
            return false;
        }

        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var programPublisherParameters =
            ProgramServiceUpdaterParameters.ProgramPublisherParameters;

        var createPackageAndUpload = new CreatePackageAndUpload(_logger, programPublisherParameters.ProjectName,
            programPublisherParameters.MainProjectFileName, programPublisherParameters.ServerInfo,
            programPublisherParameters.WorkFolder, programPublisherParameters.DateMask,
            programPublisherParameters.Runtime, programPublisherParameters.RedundantFileNames,
            programPublisherParameters.UploadTempExtension, programPublisherParameters.FileStorageForExchange,
            programPublisherParameters.SmartSchemaForLocal, programPublisherParameters.SmartSchemaForExchange);

        if (!await createPackageAndUpload.Run(cancellationToken))
            return false;


        //2. დავშიფროთ პარამეტრების ფაილი და ავტვირთოთ ფაილსაცავში
        var appSettingsEncoderParameters =
            ProgramServiceUpdaterParameters.AppSettingsEncoderParameters;
        string? appSettingsVersion = null;
        var installParameters = !string.IsNullOrWhiteSpace(appSettingsEncoderParameters.AppSettingsJsonSourceFileName);
        if (installParameters)
        {
            var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(_logger,
                appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
                appSettingsEncoderParameters.AppSettingsJsonSourceFileName,
                appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, appSettingsEncoderParameters.KeyPart1,
                appSettingsEncoderParameters.KeyPart2, appSettingsEncoderParameters.ProjectName,
                appSettingsEncoderParameters.ServerInfo, appSettingsEncoderParameters.DateMask,
                appSettingsEncoderParameters.ParametersFileExtension,
                appSettingsEncoderParameters.FileStorageForExchange, appSettingsEncoderParameters.ExchangeSmartSchema);
            if (!await encodeParametersAndUploadAction.Run(cancellationToken))
                return false;
            appSettingsVersion = encodeParametersAndUploadAction.AppSettingsVersion;
        }

        //3. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
        var installProgramAction = new InstallServiceAction(_logger,
            ProgramServiceUpdaterParameters.InstallerBaseParameters,
            ProgramServiceUpdaterParameters.ProgramArchiveDateMask,
            ProgramServiceUpdaterParameters.ProgramArchiveExtension,
            ProgramServiceUpdaterParameters.ParametersFileDateMask,
            ProgramServiceUpdaterParameters.ParametersFileExtension,
            ProgramServiceUpdaterParameters.FileStorageForDownload, projectName, environmentName,
            ProgramServiceUpdaterParameters.ServiceUserName,
            appSettingsEncoderParameters.AppSettingsEncodedJsonFileName,
            ProgramServiceUpdaterParameters.ServiceDescriptionSignature,
            ProgramServiceUpdaterParameters.ProjectDescription);
        if (!await installProgramAction.Run(cancellationToken))
        {
            _logger.LogError("project {projectName}/{environmentName} was not updated", projectName, environmentName);
            return false;
        }

        //string installingProgramVersion = installProgramAction.InstallingProgramVersion;
        //4. შევამოწმოთ, რომ გაშვებული პროგრამის ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით//, projectName
        var checkProgramVersionAction = new CheckProgramVersionAction(_logger,
            ProgramServiceUpdaterParameters.CheckVersionParameters.WebAgentForCheck,
            ProgramServiceUpdaterParameters.ProxySettings, createPackageAndUpload.AssemblyVersion);

        if (!await checkProgramVersionAction.Run(cancellationToken))
        {
            _logger.LogError("project {projectName}/{environmentName} version check failed", projectName,
                environmentName);
            return false;
        }

        if (installParameters)
        {
            //5. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
            //, projectName
            var checkParametersVersionAction = new CheckParametersVersionAction(_logger,
                ProgramServiceUpdaterParameters.CheckVersionParameters.WebAgentForCheck,
                ProgramServiceUpdaterParameters.ProxySettings, appSettingsVersion);

            if (await checkParametersVersionAction.Run(cancellationToken))
                return true;
        }

        _logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
            environmentName);
        return false;
    }
}