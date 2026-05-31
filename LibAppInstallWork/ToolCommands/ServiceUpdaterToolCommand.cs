using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using LibAppInstallWork.ToolCommands.AppSettingsPreparer;
using LibAppInstallWork.ToolCommands.ProgPublisher;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceUpdaterToolCommand : ToolCommand
{
    private const string ActionName = "Update App";
    private const string ActionDescription = "Update App";
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public ServiceUpdaterToolCommand(string appName, ILogger logger, IHttpClientFactory httpClientFactory,
        ServiceUpdaterParameters programServiceUpdaterParameters, IParametersManager parametersManager,
        bool useConsole) : base(logger, ActionName, programServiceUpdaterParameters, parametersManager,
        ActionDescription, useConsole)
    {
        _appName = appName;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private ServiceUpdaterParameters ProgramServiceUpdaterParameters => (ServiceUpdaterParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        string projectName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ProjectName;
        string? environmentName = ProgramServiceUpdaterParameters.ProgramPublisherParameters.ServerInfo.EnvironmentName;

        if (string.IsNullOrEmpty(environmentName))
        {
            _logger.LogError("Environment {EnvironmentName} is empty", environmentName);
            return false;
        }

        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        ProgramPublisherParameters programPublisherParameters =
            ProgramServiceUpdaterParameters.ProgramPublisherParameters;

        var createPackageAndUpload = new CreatePackageAndUpload(_logger, programPublisherParameters.ProjectName,
            programPublisherParameters.MainProjectFileName, programPublisherParameters.ServerInfo,
            programPublisherParameters.WorkFolder, programPublisherParameters.DateMask,
            programPublisherParameters.Runtime, programPublisherParameters.RedundantFileNames,
            programPublisherParameters.UploadTempExtension, programPublisherParameters.FileStorageForExchange,
            programPublisherParameters.SmartSchemaForLocal, programPublisherParameters.SmartSchemaForExchange);

        if (!await createPackageAndUpload.Run(cancellationToken))
        {
            return false;
        }

        var appSettingsFileName = "appsettings.json";

        //2. დავშიფროთ პარამეტრების ფაილი და ავტვირთოთ ფაილსაცავში
        AppSettingsEncoderParameters appSettingsEncoderParameters =
            ProgramServiceUpdaterParameters.AppSettingsEncoderParameters;
        AppSettingsPreparerParameters appSettingsPreparerParameters =
            ProgramServiceUpdaterParameters.AppSettingsPreparerParameters;
        string? appSettingsVersion = null;
        bool installParameters =
            !string.IsNullOrWhiteSpace(appSettingsPreparerParameters.AppSettingsJsonSourceFileName);
        if (installParameters)
        {
            if (appSettingsEncoderParameters is not null)
            {
                var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(_logger,
                    appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
                    appSettingsPreparerParameters.AppSettingsJsonSourceFileName,
                    appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, appSettingsEncoderParameters.KeyPart1,
                    appSettingsEncoderParameters.KeyPart2, appSettingsPreparerParameters.ProjectName,
                    appSettingsPreparerParameters.ServerInfo, appSettingsPreparerParameters.DateMask,
                    appSettingsPreparerParameters.ParametersFileExtension,
                    appSettingsPreparerParameters.FileStorageForExchange,
                    appSettingsPreparerParameters.ExchangeSmartSchema);
                if (!await encodeParametersAndUploadAction.Run(cancellationToken))
                {
                    return false;
                }

                appSettingsFileName = Path.GetFileName(appSettingsEncoderParameters.AppSettingsEncodedJsonFileName);
                appSettingsVersion = encodeParametersAndUploadAction.AppSettingsVersion;
            }
            else
            {
                var prepareParametersAndUploadAction = new PrepareParametersAndUploadAction(_logger,
                    appSettingsPreparerParameters.AppSettingsJsonSourceFileName,
                    appSettingsPreparerParameters.ProjectName, appSettingsPreparerParameters.ServerInfo,
                    appSettingsPreparerParameters.DateMask, appSettingsPreparerParameters.ParametersFileExtension,
                    appSettingsPreparerParameters.FileStorageForExchange,
                    appSettingsPreparerParameters.ExchangeSmartSchema);
                if (!await prepareParametersAndUploadAction.Run(cancellationToken))
                {
                    return false;
                }
            }
        }

        //3. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
        var installProgramAction = new InstallServiceAction(_appName, _logger, _httpClientFactory,
            ProgramServiceUpdaterParameters.InstallerBaseParameters,
            ProgramServiceUpdaterParameters.ProgramArchiveDateMask,
            ProgramServiceUpdaterParameters.ProgramArchiveExtension,
            ProgramServiceUpdaterParameters.ParametersFileDateMask,
            ProgramServiceUpdaterParameters.ParametersFileExtension,
            ProgramServiceUpdaterParameters.FileStorageForDownload, projectName, environmentName,
            ProgramServiceUpdaterParameters.ServiceUserName, appSettingsFileName,
            ProgramServiceUpdaterParameters.ServiceDescriptionSignature,
            ProgramServiceUpdaterParameters.ProjectDescription, UseConsole);
        if (!await installProgramAction.Run(cancellationToken))
        {
            _logger.LogError("project {ProjectName}/{EnvironmentName} was not updated", projectName, environmentName);
            return false;
        }

        //string installingProgramVersion = installProgramAction.InstallingProgramVersion;
        //4. შევამოწმოთ, რომ გაშვებული პროგრამის ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით//, projectName
        var checkProgramVersionAction = new CheckProgramVersionAction(_logger, _httpClientFactory,
            ProgramServiceUpdaterParameters.CheckVersionParameters.WebAgentForCheck,
            ProgramServiceUpdaterParameters.ProxySettings, createPackageAndUpload.AssemblyVersion, UseConsole);

        if (!await checkProgramVersionAction.Run(cancellationToken))
        {
            _logger.LogError("project {ProjectName}/{EnvironmentName} version check failed", projectName,
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
            {
                return true;
            }
        }

        _logger.LogError("project {ProjectName}/{EnvironmentName} parameters file check failed", projectName,
            environmentName);
        return false;
    }
}
