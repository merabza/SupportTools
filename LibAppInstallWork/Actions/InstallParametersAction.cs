﻿using System.IO;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.Actions;

public sealed class InstallParametersAction : ToolAction
{
    private readonly string _appSettingsEncodedJsonFileName;
    private readonly FileStorageData _fileStorageForUpload;
    private readonly InstallerBaseParameters _installerBaseParameters;
    private readonly string _parametersFileDateMask;
    private readonly string _parametersFileExtension;
    private readonly string _projectName;
    private readonly string? _serviceName;

    public InstallParametersAction(ILogger logger, bool useConsole, string parametersFileDateMask,
        string parametersFileExtension, InstallerBaseParameters installerBaseParameters,
        FileStorageData fileStorageForUpload, string projectName, string? serviceName,
        string appSettingsEncodedJsonFileName) : base(logger, useConsole, "Install Parameters")
    {
        _installerBaseParameters = installerBaseParameters;
        _parametersFileDateMask = parametersFileDateMask;
        _parametersFileExtension = parametersFileExtension;
        _fileStorageForUpload = fileStorageForUpload;
        _projectName = projectName;
        _serviceName = serviceName;
        _appSettingsEncodedJsonFileName = appSettingsEncodedJsonFileName;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //კლიენტის შექმნა
        var agentClient =
            AgentClientsFabricExt.CreateAgentClientWithFileStorage(Logger, _fileStorageForUpload,
                _installerBaseParameters);
        if (agentClient is null)
        {
            Logger.LogError($"agentClient cannot be created. project {_projectName} does not updated");
            return false;
        }

        Logger.LogInformation($"Updating app settings for project {_projectName} by web agent...");
        //Web-აგენტის საშუალებით პარამეტრების ფაილის განახლების პროცესის გაშვება.

        if (agentClient.UpdateAppParametersFile(_projectName, _serviceName,
                Path.GetFileName(_appSettingsEncodedJsonFileName), _parametersFileDateMask, _parametersFileExtension))
            return true;

        Logger.LogError($"project {_projectName} does not updated");
        return false;
    }
}