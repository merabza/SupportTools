using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramUpdater : ToolCommand
{
    private readonly ILogger _logger;
    private const string ActionName = "Update App";
    private const string ActionDescription = "Update App";

    public ProgramUpdater(ILogger logger, ProgramUpdaterParameters programUpdaterParameters,
        IParametersManager parametersManager) : base(logger, ActionName, programUpdaterParameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
    }

    private ProgramUpdaterParameters ProgramUpdaterParameters => (ProgramUpdaterParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
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
        var installProgramAction = new InstallProgramAction(_logger, ProgramUpdaterParameters.InstallerBaseParameters,
            ProgramUpdaterParameters.ProgramArchiveDateMask, ProgramUpdaterParameters.ProgramArchiveExtension,
            ProgramUpdaterParameters.ParametersFileDateMask, ProgramUpdaterParameters.ParametersFileExtension,
            ProgramUpdaterParameters.FileStorageForDownload, projectName, environmentName);

        if (await installProgramAction.Run(cancellationToken))
            return true;

        _logger.LogError("project {projectName} not updated", projectName);
        return false;
    }
}