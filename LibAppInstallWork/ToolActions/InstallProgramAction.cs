using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SystemToolsShared.Errors;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class InstallProgramAction : ToolAction
{
    private readonly string _environmentName;
    private readonly FileStorageData _fileStorageForDownload;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly ILogger _logger;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _programArchiveDateMask;
    private readonly string _programArchiveExtension;
    private readonly string _projectName;

    private string? _installingProgramVersion;


    public InstallProgramAction(ILogger logger, IHttpClientFactory httpClientFactory,
        InstallerBaseParameters installerBaseParameters, string programArchiveDateMask, string programArchiveExtension,
        string parametersFileDateMask, string parametersFileExtension, FileStorageData fileStorageForDownload,
        string projectName, string environmentName, bool useConsole) : base(logger, "Install service", null, null,
        useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _installerBaseParameters = installerBaseParameters;
        _programArchiveDateMask = programArchiveDateMask;
        _programArchiveExtension = programArchiveExtension;
        _parametersFileDateMask = parametersFileDateMask;
        _parametersFileExtension = parametersFileExtension;
        _fileStorageForDownload = fileStorageForDownload;
        _projectName = projectName;
        _environmentName = environmentName;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //კლიენტის შექმნა
        var projectManager = ProjectsManagersFabric.CreateProjectsManagerWithFileStorage(_logger, _httpClientFactory,
            _fileStorageForDownload, _installerBaseParameters, UseConsole);

        if (projectManager is null)
        {
            _logger.LogError("agentClient does not created. project {_projectName}/{_environmentName} does not updated",
                _projectName, _environmentName);
            return false;
        }

        _logger.LogInformation("Installing {_projectName}/{_environmentName} by web agent...", _projectName,
            _environmentName);

        //Web-აგენტის საშუალებით ინსტალაციის პროცესის გაშვება.
        var installProgramResult = await projectManager.InstallProgram(_projectName, _environmentName,
            _programArchiveDateMask, _programArchiveExtension, _parametersFileDateMask, _parametersFileExtension,
            cancellationToken);

        if (installProgramResult.IsT1)
        {
            _logger.LogError("Error when Install program project {_projectName}/{_environmentName}", _projectName,
                _environmentName);
            Err.PrintErrorsOnConsole(installProgramResult.AsT1);
            return false;
        }

        _installingProgramVersion = installProgramResult.AsT0;

        if (_installingProgramVersion != null)
            return true;

        _logger.LogError("project {_projectName}/{_environmentName} does not updated", _projectName, _environmentName);
        return false;
    }
}