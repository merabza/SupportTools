using System;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.Actions;

public sealed class InstallProgramAction : ToolAction
{
    private readonly string _environmentName;
    private readonly FileStorageData _fileStorageForDownload;
    private readonly ILogger _logger;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _programArchiveDateMask;
    private readonly string _programArchiveExtension;
    private readonly string _projectName;

    private string? _installingProgramVersion;


    public InstallProgramAction(ILogger logger, InstallerBaseParameters installerBaseParameters,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, FileStorageData fileStorageForDownload, string projectName,
        string environmentName) : base(logger, "Install service", null, null)
    {
        _logger = logger;
        _installerBaseParameters = installerBaseParameters;
        _programArchiveDateMask = programArchiveDateMask;
        _programArchiveExtension = programArchiveExtension;
        _parametersFileDateMask = parametersFileDateMask;
        _parametersFileExtension = parametersFileExtension;
        _fileStorageForDownload = fileStorageForDownload;
        _projectName = projectName;
        _environmentName = environmentName;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        //კლიენტის შექმნა
        var agentClient =
            ProjectsAgentClientsFabric.CreateProjectsApiClientWithFileStorage(_logger, _fileStorageForDownload,
                _installerBaseParameters);

        if (agentClient is null)
        {
            _logger.LogError("agentClient does not created. project {_projectName}/{_environmentName} does not updated",
                _projectName, _environmentName);
            return false;
        }

        _logger.LogInformation("Installing {_projectName}/{_environmentName} by web agent...", _projectName,
            _environmentName);

        //Web-აგენტის საშუალებით ინსტალაციის პროცესის გაშვება.
        var installProgramResult = await agentClient.InstallProgram(_projectName, _environmentName,
            _programArchiveDateMask,
            _programArchiveExtension, _parametersFileDateMask, _parametersFileExtension, cancellationToken);


        if (agentClient is IDisposable disposable)
            disposable.Dispose();

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