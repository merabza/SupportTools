using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceUpdaterToolCommand : ToolCommand
{
    private const string ActionName = "Update App";
    private const string ActionDescription = "Update App";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public ServiceUpdaterToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ServiceUpdaterParameters programServiceUpdaterParameters, IParametersManager parametersManager,
        bool useConsole) : base(logger, ActionName, programServiceUpdaterParameters, parametersManager,
        ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private ServiceUpdaterParameters ProgramServiceUpdaterParameters => (ServiceUpdaterParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var projectName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ProjectName;
        var environmentName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ServerInfo.EnvironmentName;

        if (string.IsNullOrEmpty(environmentName))
        {
            _logger.LogError("Environment {environmentName} is empty", environmentName);
            return false;
        }

        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var programPublisherParameters = ProgramServiceUpdaterParameters.ProgramPublisherParameters;

        var createPackageAndUpload = new CreatePackageAndUpload(_logger, programPublisherParameters.ProjectName,
            programPublisherParameters.MainProjectFileName, programPublisherParameters.ServerInfo,
            programPublisherParameters.WorkFolder, programPublisherParameters.DateMask,
            programPublisherParameters.Runtime, programPublisherParameters.RedundantFileNames,
            programPublisherParameters.UploadTempExtension, programPublisherParameters.FileStorageForExchange,
            programPublisherParameters.SmartSchemaForLocal, programPublisherParameters.SmartSchemaForExchange);

        if (!await createPackageAndUpload.Run(cancellationToken))
            return false;


        //2. დავშიფროთ პარამეტრების ფაილი და ავტვირთოთ ფაილსაცავში
        var appSettingsEncoderParameters = ProgramServiceUpdaterParameters.AppSettingsEncoderParameters;
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
        var installProgramAction = new InstallServiceAction(_logger, _httpClientFactory,
            ProgramServiceUpdaterParameters.InstallerBaseParameters,
            ProgramServiceUpdaterParameters.ProgramArchiveDateMask,
            ProgramServiceUpdaterParameters.ProgramArchiveExtension,
            ProgramServiceUpdaterParameters.ParametersFileDateMask,
            ProgramServiceUpdaterParameters.ParametersFileExtension,
            ProgramServiceUpdaterParameters.FileStorageForDownload, projectName, environmentName,
            ProgramServiceUpdaterParameters.ServiceUserName,
            appSettingsEncoderParameters.AppSettingsEncodedJsonFileName,
            ProgramServiceUpdaterParameters.ServiceDescriptionSignature,
            ProgramServiceUpdaterParameters.ProjectDescription, UseConsole);
        if (!await installProgramAction.Run(cancellationToken))
        {
            _logger.LogError("project {projectName}/{environmentName} was not updated", projectName, environmentName);
            return false;
        }

        //string installingProgramVersion = installProgramAction.InstallingProgramVersion;
        //4. შევამოწმოთ, რომ გაშვებული პროგრამის ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით//, projectName
        var checkProgramVersionAction = new CheckProgramVersionAction(_logger, _httpClientFactory,
            ProgramServiceUpdaterParameters.CheckVersionParameters.WebAgentForCheck,
            ProgramServiceUpdaterParameters.ProxySettings, createPackageAndUpload.AssemblyVersion, UseConsole);

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
            var checkParametersVersionAction = new CheckParametersVersionAction(_logger, _httpClientFactory,
                ProgramServiceUpdaterParameters.CheckVersionParameters.WebAgentForCheck,
                ProgramServiceUpdaterParameters.ProxySettings, appSettingsVersion, 10, UseConsole);

            if (await checkParametersVersionAction.Run(cancellationToken))
                return true;
        }

        _logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
            environmentName);
        return false;
    }
}