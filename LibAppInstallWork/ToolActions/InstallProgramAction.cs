using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.Installer.ProjectManagers;
using ToolsManagement.LibToolActions;

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

    //private string? _installingProgramVersion;

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
        IIProjectsManagerWithFileStorage? projectManager =
            ProjectsManagersFactory.CreateProjectsManagerWithFileStorage(_logger, _httpClientFactory,
                _fileStorageForDownload, _installerBaseParameters, UseConsole);

        if (projectManager is null)
        {
            _logger.LogError("agentClient does not created. project {ProjectName}/{EnvironmentName} does not updated",
                _projectName, _environmentName);
            return false;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Installing {ProjectName}/{EnvironmentName} by web agent...", _projectName,
                _environmentName);
        }

        //Web-აგენტის საშუალებით ინსტალაციის პროცესის გაშვება.
        OneOf<string, Err[]> installProgramResult = await projectManager.InstallProgram(_projectName, _environmentName,
            _programArchiveDateMask, _programArchiveExtension, _parametersFileDateMask, _parametersFileExtension,
            cancellationToken);

        if (installProgramResult.IsT1)
        {
            _logger.LogError("Error when Install program project {ProjectName}/{EnvironmentName}", _projectName,
                _environmentName);
            Err.PrintErrorsOnConsole(installProgramResult.AsT1);
            return false;
        }

        if (installProgramResult.AsT0 is not null)
        {
            return true;
        }

        _logger.LogError("project {ProjectName}/{EnvironmentName} does not updated", _projectName, _environmentName);
        return false;
    }
}
