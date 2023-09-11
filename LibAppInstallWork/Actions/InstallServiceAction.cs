using System.IO;
using System.Threading;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.Actions;

public sealed class InstallServiceAction : ToolAction
{
    private readonly string _encodedJsonFileName;
    private readonly string _environmentName;
    private readonly FileStorageData _fileStorageForDownload;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _programArchiveDateMask;
    private readonly string _programArchiveExtension;
    private readonly string _projectName;
    private readonly string? _serviceName;
    private readonly string _serviceUserName;


    public InstallServiceAction(ILogger logger, InstallerBaseParameters installerBaseParameters,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, FileStorageData fileStorageForDownload, string projectName,
        string environmentName, string? serviceName, string serviceUserName, string encodedJsonFileName) : base(logger,
        "Install service", null, null)
    {
        _installerBaseParameters = installerBaseParameters;
        _programArchiveDateMask = programArchiveDateMask;
        _programArchiveExtension = programArchiveExtension;
        _parametersFileDateMask = parametersFileDateMask;
        _parametersFileExtension = parametersFileExtension;
        _fileStorageForDownload = fileStorageForDownload;
        _projectName = projectName;
        _environmentName = environmentName;
        _serviceName = serviceName;
        _serviceUserName = serviceUserName;
        _encodedJsonFileName = encodedJsonFileName;
    }

    public string? InstallingProgramVersion { get; private set; }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //კლიენტის შექმნა
        var agentClient =
            ProjectsAgentClientsFabric.CreateProjectsApiClientWithFileStorage(Logger, _fileStorageForDownload,
                _installerBaseParameters);

        if (agentClient is null)
        {
            Logger.LogError(
                "agentClient does not created. project {_projectName}/{_environmentName} does not updated",
                _projectName, _environmentName);
            return false;
        }

        Logger.LogInformation("Installing {_projectName}/{_environmentName} by web agent...", _projectName,
            _environmentName);

        //Web-აგენტის საშუალებით ინსტალაციის პროცესის გაშვება.
        InstallingProgramVersion = agentClient.InstallService(_projectName, _environmentName, _serviceName,
            _serviceUserName, Path.GetFileName(_encodedJsonFileName), _programArchiveDateMask, _programArchiveExtension,
            _parametersFileDateMask, _parametersFileExtension, CancellationToken.None).Result;

        if (InstallingProgramVersion != null)
            return true;

        Logger.LogError("project {_projectName}/{_environmentName} does not updated", _projectName, _environmentName);
        return false;
    }
}