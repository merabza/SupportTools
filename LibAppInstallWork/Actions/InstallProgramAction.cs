using LibAppInstallWork.Models;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.Actions;

public sealed class InstallProgramAction : ToolAction
{
    private readonly FileStorageData _fileStorageForDownload;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _programArchiveDateMask;
    private readonly string _programArchiveExtension;
    private readonly string _projectName;

    private string? _installingProgramVersion;
    //public string? InstallingProgramVersion => _installingProgramVersion;


    public InstallProgramAction(ILogger logger, bool useConsole, InstallerBaseParameters installerBaseParameters,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, FileStorageData fileStorageForDownload, string projectName) : base(logger,
        useConsole, "Install service")
    {
        _installerBaseParameters = installerBaseParameters;
        _programArchiveDateMask = programArchiveDateMask;
        _programArchiveExtension = programArchiveExtension;
        _parametersFileDateMask = parametersFileDateMask;
        _parametersFileExtension = parametersFileExtension;
        _fileStorageForDownload = fileStorageForDownload;
        _projectName = projectName;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //კლიენტის შექმნა
        var agentClient =
            AgentClientsFabricExt.CreateAgentClientWithFileStorage(Logger, _fileStorageForDownload,
                _installerBaseParameters);

        if (agentClient is null)
        {
            Logger.LogError($"agentClient does not created. project {_projectName} does not updated");
            return false;
        }

        Logger.LogInformation($"Installing {_projectName} by web agent...");

        //Web-აგენტის საშუალებით ინსტალაციის პროცესის გაშვება.
        _installingProgramVersion = agentClient.InstallProgram(_projectName, _programArchiveDateMask,
            _programArchiveExtension, _parametersFileDateMask, _parametersFileExtension);

        if (_installingProgramVersion != null)
            return true;

        Logger.LogError($"project {_projectName} does not updated");
        return false;
    }
}