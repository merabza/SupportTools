using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibMenuInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.ToolActions;
using SupportTools.ToolCommandParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.ToolCommands;

public sealed class ServiceInstallScriptCreatorToolCommand : ToolCommand
{
    private const string ActionName = "Creating Service Install Script";
    private const string ActionDescription = "Creating Service Install Script";
    private readonly ILogger _logger;
    private readonly ServiceInstallScriptCreatorParameters _par;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServiceInstallScriptCreatorToolCommand(ILogger logger, ServiceInstallScriptCreatorParameters par,
        IParametersManager? parametersManager) : base(logger, ActionName, par, parametersManager, ActionDescription)
    {
        _logger = logger;
        _par = par;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var ftpSiteUserName = _par.FileStorageForExchange.UserName;

        if (string.IsNullOrWhiteSpace(ftpSiteUserName))
        {
            _logger.LogError("ftp site user name is not specified for FileStorageForExchange");
            return false;
        }

        var fileStoragePath = _par.FileStorageForExchange.FileStoragePath;

        var isFtp = _par.FileStorageForExchange.IsFtp();
        if (isFtp is null)
        {
            _logger.LogError("could not be determined File Storage {fileStoragePath} is ftp file storage or not",
                fileStoragePath);
            return false;
        }

        if (!isFtp.Value)
        {
            _logger.LogError("File Storage {fileStoragePath} is not ftp file storage", fileStoragePath);
            return false;
        }

        if (!Uri.TryCreate(fileStoragePath, UriKind.Absolute, out var uri))
        {
            _logger.LogError("Invalid File Storage Path {fileStoragePath}", fileStoragePath);
            return false;
        }

        var hostName = uri.Host;
        var startPath = uri.AbsolutePath;
        var port = uri.Port;

        var ftpSiteAddress = port == 21 ? hostName : $"\"{hostName} {port}\"";

        var userName = _par.FileStorageForExchange.UserName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogError("User Name is not specified for File Storage {fileStoragePath}", fileStoragePath);
            return false;
        }

        var password = _par.FileStorageForExchange.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogError("Password is not specified for File Storage {fileStoragePath}", fileStoragePath);
            return false;
        }

        if (_par.FileStorageForExchange.FtpSiteLsFileOffset == 0)
        {
            _logger.LogError("FtpSiteLsFileOffset is not specified for File Storage {fileStoragePath}",
                fileStoragePath);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_par.ServerInfo.ServerName))
        {
            StShared.WriteErrorLine(
                $"ServerName is not specified for server {_par.ServerInfo.GetItemKey()} and project {_par.ProjectName}",
                true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_par.ServerInfo.ServiceUserName))
        {
            StShared.WriteErrorLine(
                $"ServiceUserName is not specified for server {_par.ServerInfo.GetItemKey()} and project {_par.ProjectName}",
                true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_par.ServerInfo.EnvironmentName))
        {
            StShared.WriteErrorLine(
                $"EnvironmentName is not specified for server {_par.ServerInfo.GetItemKey()} and project {_par.ProjectName}",
                true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_par.ServerInfo.AppSettingsEncodedJsonFileName))
        {
            StShared.WriteErrorLine(
                $"App Settings Encoded Json File Name is not specified for server {_par.ServerInfo.GetItemKey()} and project {_par.ProjectName}",
                true);
            return false;
        }

        var securityFolder = _par.SecurityFolder;
        string? defCloneFile = null;
        if (securityFolder is not null)
            defCloneFile = Path.Combine(securityFolder, _par.ProjectName, _par.ServerInfo.ServerName,
                _par.ServerInfo.EnvironmentName, $"{_par.ProjectName}Install.sh");
        var scriptFileNameForSave = MenuInputer.InputFilePath("File name for Generate", defCloneFile, false);
        if (scriptFileNameForSave is null)
        {
            StShared.WriteErrorLine("file name for Generate is not specified", true);
            return false;
        }

        if (ParametersManager is null)
        {
            StShared.WriteErrorLine("ParametersManager is null", true);
            return false;
        }

        var supportToolsParameters = (SupportToolsParameters)ParametersManager.Parameters;
        var serverData = supportToolsParameters.GetServerDataRequired(_par.ServerInfo.ServerName);

        if (string.IsNullOrWhiteSpace(serverData.Runtime))
        {
            StShared.WriteErrorLine($"serverData.Runtime is not specified for server {_par.ServerInfo.ServerName}",
                true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(serverData.ServerSideDownloadFolder))
        {
            StShared.WriteErrorLine(
                $"serverData.ServerSideDownloadFolder is not specified for server {_par.ServerInfo.ServerName}", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(serverData.ServerSideDeployFolder))
        {
            StShared.WriteErrorLine(
                $"serverData.ServerSideDeployFolder is not specified for server {_par.ServerInfo.ServerName}", true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(serverData.FilesUserName))
        {
            StShared.WriteErrorLine(
                $"serverData.FilesUserName is not specified for server {_par.ServerInfo.ServerName}", true);
            return false;
        }

        var sf = new FileInfo(_par.ServerInfo.AppSettingsEncodedJsonFileName);

        var createInstallScript = new CreateServiceInstallScript(_logger, scriptFileNameForSave,
            _par.ServerInfo.ServerSidePort, ftpSiteAddress, userName, password, startPath, _par.ProjectName,
            _par.ServiceDescriptionSignature, _par.Project.ProjectDescription, serverData.Runtime,
            _par.ServerInfo.EnvironmentName, serverData.ServerSideDownloadFolder, serverData.ServerSideDeployFolder,
            sf.Name, _par.ServerInfo.ServiceUserName, _par.FileStorageForExchange.FtpSiteLsFileOffset);
        return await createInstallScript.Run(cancellationToken);
    }
}