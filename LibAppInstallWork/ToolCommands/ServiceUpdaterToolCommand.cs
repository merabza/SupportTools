using System.Collections.Generic;
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
    private const string AppSettingsFileName = "appsettings.json";
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

        //OpenPort-ის ჩართვისას გასახსნელი პორტები; შეცდომა პაკეტის აგებამდე ჩანს
        List<int>? portsToOpen = KestrelPortsReader.GetPortsToOpen(
            ProgramServiceUpdaterParameters.ProgramPublisherParameters.ServerInfo, projectName);
        if (portsToOpen is null)
        {
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

        string appSettingsFileName = AppSettingsFileName;

        //2. დავშიფროთ პარამეტრების ფაილი და ავტვირთოთ ფაილსაცავში
        AppSettingsPreparerParameters appSettingsPreparerParameters =
            ProgramServiceUpdaterParameters.AppSettingsPreparerParameters;
        string? appSettingsVersion = null;
        bool installParameters =
            !string.IsNullOrWhiteSpace(appSettingsPreparerParameters.AppSettingsJsonSourceFileName);
        if (installParameters)
        {
            UploadedAppSettings? uploadedAppSettings = await UploadAppSettings(_logger, appSettingsPreparerParameters,
                ProgramServiceUpdaterParameters.AppSettingsEncoderParameters, cancellationToken);
            if (uploadedAppSettings is null)
            {
                return false;
            }

            appSettingsFileName = uploadedAppSettings.FileName;
            appSettingsVersion = uploadedAppSettings.Version;
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
            ProgramServiceUpdaterParameters.ProjectDescription, portsToOpen, UseConsole);
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

    //პარამეტრების ფაილის დაშიფვრა ან მომზადება და ფაილსაცავში ატვირთვა. აბრუნებს სერვისისთვის გადასაცემი ფაილის
    //სახელს და ატვირთულ ფაილში ჩაწერილ ვერსიას, რომელსაც პარამეტრების შემოწმება ადარებს; შეცდომისას null
    internal static async ValueTask<UploadedAppSettings?> UploadAppSettings(ILogger logger,
        AppSettingsPreparerParameters appSettingsPreparerParameters,
        AppSettingsEncoderParameters? appSettingsEncoderParameters, CancellationToken cancellationToken = default)
    {
        if (appSettingsEncoderParameters is not null)
        {
            var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(logger,
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
                return null;
            }

            return new UploadedAppSettings(
                Path.GetFileName(appSettingsEncoderParameters.AppSettingsEncodedJsonFileName),
                encodeParametersAndUploadAction.AppSettingsVersion);
        }

        var prepareParametersAndUploadAction = new PrepareParametersAndUploadAction(logger,
            appSettingsPreparerParameters.AppSettingsJsonSourceFileName, appSettingsPreparerParameters.ProjectName,
            appSettingsPreparerParameters.ServerInfo, appSettingsPreparerParameters.DateMask,
            appSettingsPreparerParameters.ParametersFileExtension, appSettingsPreparerParameters.FileStorageForExchange,
            appSettingsPreparerParameters.ExchangeSmartSchema);
        if (!await prepareParametersAndUploadAction.Run(cancellationToken))
        {
            return null;
        }

        return new UploadedAppSettings(AppSettingsFileName, prepareParametersAndUploadAction.AppSettingsVersion);
    }
}
