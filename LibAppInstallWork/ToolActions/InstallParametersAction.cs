using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class InstallParametersAction : ToolAction
{
    private readonly string _appSettingsEncodedJsonFileName;
    private readonly string _environmentName;
    private readonly FileStorageData _fileStorageForUpload;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly ILogger _logger;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;

    public InstallParametersAction(ILogger logger, IHttpClientFactory httpClientFactory, string parametersFileDateMask,
        string parametersFileExtension, InstallerBaseParameters installerBaseParameters,
        FileStorageData fileStorageForUpload, string projectName, string environmentName,
        string appSettingsEncodedJsonFileName, bool useConsole) : base(logger, "Install Parameters", null, null,
        useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _installerBaseParameters = installerBaseParameters;
        _parametersFileDateMask = parametersFileDateMask;
        _parametersFileExtension = parametersFileExtension;
        _fileStorageForUpload = fileStorageForUpload;
        _projectName = projectName;
        _environmentName = environmentName;
        _appSettingsEncodedJsonFileName = appSettingsEncodedJsonFileName;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var projectManager = ProjectsManagersFactory.CreateProjectsManagerWithFileStorage(_logger, _httpClientFactory,
            _fileStorageForUpload, _installerBaseParameters, UseConsole);
        if (projectManager is null)
        {
            _logger.LogError(
                "agentClient cannot be created. project {_projectName}/{_environmentName} does not updated",
                _projectName, _environmentName);
            return false;
        }

        _logger.LogInformation("Updating app settings for project {_projectName}/{_environmentName} by web agent...",
            _projectName, _environmentName);
        //Web-აგენტის საშუალებით პარამეტრების ფაილის განახლების პროცესის გაშვება.

        var updateAppParametersFileResult = await projectManager.UpdateAppParametersFile(_projectName, _environmentName,
            Path.GetFileName(_appSettingsEncodedJsonFileName), _parametersFileDateMask, _parametersFileExtension,
            cancellationToken);

        if (updateAppParametersFileResult.IsNone)
            return true;

        _logger.LogError("project {_projectName}/{_environmentName} does not updated", _projectName, _environmentName);
        return false;
    }
}