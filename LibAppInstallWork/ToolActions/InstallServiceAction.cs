using System.IO;
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

public sealed class InstallServiceAction : ToolAction
{
    private readonly string _encodedJsonFileName;
    private readonly string _environmentName;
    private readonly FileStorageData _fileStorageForDownload;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly ILogger _logger;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _programArchiveDateMask;
    private readonly string _programArchiveExtension;
    private readonly string? _projectDescription;
    private readonly string _projectName;
    private readonly string? _serviceDescriptionSignature;
    private readonly string _serviceUserName;

    public InstallServiceAction(ILogger logger, IHttpClientFactory httpClientFactory,
        InstallerBaseParameters installerBaseParameters, string programArchiveDateMask, string programArchiveExtension,
        string parametersFileDateMask, string parametersFileExtension, FileStorageData fileStorageForDownload,
        string projectName, string environmentName, string serviceUserName, string encodedJsonFileName,
        string? serviceDescriptionSignature, string? projectDescription, bool useConsole) : base(logger,
        "Install service", null, null, useConsole)
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
        _serviceUserName = serviceUserName;
        _encodedJsonFileName = encodedJsonFileName;
        _serviceDescriptionSignature = serviceDescriptionSignature;
        _projectDescription = projectDescription;
    }

    public string? InstallingProgramVersion { get; private set; }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //კლიენტის შექმნა
        var projectManager = ProjectsManagersFactory.CreateProjectsManagerWithFileStorage(_logger, _httpClientFactory,
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
        var installServiceResult = await projectManager.InstallService(_projectName, _environmentName, _serviceUserName,
            Path.GetFileName(_encodedJsonFileName), _programArchiveDateMask, _programArchiveExtension,
            _parametersFileDateMask, _parametersFileExtension, _serviceDescriptionSignature, _projectDescription,
            cancellationToken);

        if (installServiceResult.IsT1)
        {
            _logger.LogError("Error when Install service project {_projectName}/{_environmentName}", _projectName,
                _environmentName);
            Err.PrintErrorsOnConsole(installServiceResult.AsT1);
            return false;
        }

        InstallingProgramVersion = installServiceResult.AsT0;

        if (InstallingProgramVersion != null)
            return true;

        _logger.LogError("project {_projectName}/{_environmentName} does not updated", _projectName, _environmentName);
        return false;
    }
}