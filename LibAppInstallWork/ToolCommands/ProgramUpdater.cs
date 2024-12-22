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

public sealed class ProgramUpdater : ToolCommand
{
    private const string ActionName = "Update App";
    private const string ActionDescription = "Update App";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public ProgramUpdater(ILogger logger, IHttpClientFactory httpClientFactory,
        ProgramUpdaterParameters programUpdaterParameters, IParametersManager parametersManager,
        bool useConsole) : base(logger,
        ActionName, programUpdaterParameters, parametersManager, ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private ProgramUpdaterParameters ProgramUpdaterParameters => (ProgramUpdaterParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var projectName = ProgramUpdaterParameters.ProgramPublisherParameters.ProjectName;
        var environmentName = ProgramUpdaterParameters.ProgramPublisherParameters.ServerInfo.EnvironmentName;

        if (string.IsNullOrEmpty(environmentName))
        {
            _logger.LogError("Environment {environmentName} is empty", environmentName);
            return false;
        }

        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var programPublisherParameters = ProgramUpdaterParameters.ProgramPublisherParameters;

        var createPackageAndUpload = new CreatePackageAndUpload(_logger, programPublisherParameters.ProjectName,
            programPublisherParameters.MainProjectFileName, programPublisherParameters.ServerInfo,
            programPublisherParameters.WorkFolder, programPublisherParameters.DateMask,
            programPublisherParameters.Runtime, programPublisherParameters.RedundantFileNames,
            programPublisherParameters.UploadTempExtension, programPublisherParameters.FileStorageForExchange,
            programPublisherParameters.SmartSchemaForLocal, programPublisherParameters.SmartSchemaForExchange);

        if (!await createPackageAndUpload.Run(cancellationToken))
            return false;

        //3. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
        var installProgramAction = new InstallProgramAction(_logger, _httpClientFactory,
            ProgramUpdaterParameters.InstallerBaseParameters, ProgramUpdaterParameters.ProgramArchiveDateMask,
            ProgramUpdaterParameters.ProgramArchiveExtension, ProgramUpdaterParameters.ParametersFileDateMask,
            ProgramUpdaterParameters.ParametersFileExtension, ProgramUpdaterParameters.FileStorageForDownload,
            projectName, environmentName, UseConsole);

        if (await installProgramAction.Run(cancellationToken))
            return true;

        _logger.LogError("project {projectName} not updated", projectName);
        return false;
    }
}