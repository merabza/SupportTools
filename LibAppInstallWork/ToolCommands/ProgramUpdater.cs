using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramUpdater : ToolCommand
{
    private const string ActionName = "Update App";
    private const string ActionDescription = "Update App";

    public ProgramUpdater(ILogger logger, ProgramUpdaterParameters programUpdaterParameters,
        IParametersManager parametersManager) : base(logger, ActionName, programUpdaterParameters, parametersManager,
        ActionDescription)
    {
    }

    private ProgramUpdaterParameters ProgramUpdaterParameters => (ProgramUpdaterParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var projectName = ProgramUpdaterParameters.ProgramPublisherParameters.ProjectName;
        var environmentName = ProgramUpdaterParameters.ProgramPublisherParameters.ServerInfo.EnvironmentName;

        if (string.IsNullOrEmpty(environmentName))
        {
            Logger.LogError("Environment {environmentName} is empty", environmentName);
            return false;
        }

        //1. შევქმნათ საინსტალაციო პაკეტი და ავტვირთოთ ფაილსაცავში
        var programPublisherParameters = ProgramUpdaterParameters.ProgramPublisherParameters;

        var createPackageAndUpload = new CreatePackageAndUpload(Logger, programPublisherParameters.ProjectName,
            programPublisherParameters.MainProjectFileName, programPublisherParameters.ServerInfo,
            programPublisherParameters.WorkFolder, programPublisherParameters.DateMask,
            programPublisherParameters.Runtime, programPublisherParameters.RedundantFileNames,
            programPublisherParameters.UploadTempExtension, programPublisherParameters.FileStorageForExchange,
            programPublisherParameters.SmartSchemaForLocal, programPublisherParameters.SmartSchemaForExchange);

        if (!createPackageAndUpload.Run())
            return false;

        //3. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
        var installProgramAction = new InstallProgramAction(Logger, ProgramUpdaterParameters.InstallerBaseParameters,
            ProgramUpdaterParameters.ProgramArchiveDateMask, ProgramUpdaterParameters.ProgramArchiveExtension,
            ProgramUpdaterParameters.ParametersFileDateMask, ProgramUpdaterParameters.ParametersFileExtension,
            ProgramUpdaterParameters.FileStorageForDownload, projectName, environmentName);

        if (installProgramAction.Run())
            return true;

        Logger.LogError("project {projectName} not updated", projectName);
        return false;
    }
}