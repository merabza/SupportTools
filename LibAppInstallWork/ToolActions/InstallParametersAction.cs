﻿using System;
using System.IO;
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
    private readonly ILogger _logger;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;

    public InstallParametersAction(ILogger logger, string parametersFileDateMask, string parametersFileExtension,
        InstallerBaseParameters installerBaseParameters, FileStorageData fileStorageForUpload, string projectName,
        string environmentName, string appSettingsEncodedJsonFileName) : base(logger, "Install Parameters", null, null)
    {
        _logger = logger;
        _installerBaseParameters = installerBaseParameters;
        _parametersFileDateMask = parametersFileDateMask;
        _parametersFileExtension = parametersFileExtension;
        _fileStorageForUpload = fileStorageForUpload;
        _projectName = projectName;
        _environmentName = environmentName;
        _appSettingsEncodedJsonFileName = appSettingsEncodedJsonFileName;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        //კლიენტის შექმნა
        var agentClient =
            ProjectsAgentClientsFabric.CreateProjectsApiClientWithFileStorage(_logger, _fileStorageForUpload,
                _installerBaseParameters);
        if (agentClient is null)
        {
            _logger.LogError(
                "agentClient cannot be created. project {_projectName}/{_environmentName} does not updated",
                _projectName, _environmentName);
            return false;
        }

        _logger.LogInformation("Updating app settings for project {_projectName}/{_environmentName} by web agent...",
            _projectName, _environmentName);
        //Web-აგენტის საშუალებით პარამეტრების ფაილის განახლების პროცესის გაშვება.

        var updateAppParametersFileResult = await agentClient.UpdateAppParametersFile(_projectName, _environmentName,
            Path.GetFileName(_appSettingsEncodedJsonFileName), _parametersFileDateMask, _parametersFileExtension,
            CancellationToken.None);

        if (agentClient is IDisposable disposable)
            disposable.Dispose();

        if (updateAppParametersFileResult.IsNone)
            return true;

        _logger.LogError("project {_projectName}/{_environmentName} does not updated", _projectName, _environmentName);
        return false;
    }
}