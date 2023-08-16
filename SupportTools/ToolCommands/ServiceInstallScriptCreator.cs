using CliParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Actions;
using SupportTools.ToolCommandParameters;
using System;
using System.IO;
using LibMenuInput;
using SystemToolsShared;
using SupportToolsData.Models;

namespace SupportTools.ToolCommands;

public class ServiceInstallScriptCreator : ToolCommand
{
    private readonly ServiceInstallScriptCreatorParameters _par;
    private const string ActionName = "Creating Service Install Script";
    private const string ActionDescription = "Creating Service Install Script";

    public ServiceInstallScriptCreator(ILogger logger, bool useConsole, ServiceInstallScriptCreatorParameters par,
        IParametersManager? parametersManager) : base(logger, useConsole, ActionName, par, parametersManager,
        ActionDescription)
    {
        _par = par;
    }


    protected override bool RunAction()
    {
        var ftpSiteUserName = _par.FileStorageForExchange.UserName;

        if (string.IsNullOrWhiteSpace(ftpSiteUserName))
        {
            Logger.LogError("ftp site user name is not specified for FileStorageForExchange");
            return false;
        }

        var fileStoragePath = _par.FileStorageForExchange.FileStoragePath;
        if (!_par.FileStorageForExchange.IsFtp())
        {
            Logger.LogError("File Storage {fileStoragePath} is not ftp file storage", fileStoragePath);
            return false;
        }

        if (!Uri.TryCreate(fileStoragePath, UriKind.Absolute, out var uri))
        {
            Logger.LogError("Invalid File Storage Path {fileStoragePath}", fileStoragePath);
            return false;
        }

        var hostName = uri.Host;
        var startPath = uri.AbsolutePath;
        var port = uri.Port;

        var ftpSiteAddress = (port == 21) ? hostName : $"\"{hostName} {port}\"";

        var userName = _par.FileStorageForExchange.UserName;
        if (string.IsNullOrWhiteSpace(userName))
        {
            Logger.LogError("User Name is not specified for File Storage {fileStoragePath}", fileStoragePath);
            return false;
        }

        var password = _par.FileStorageForExchange.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            Logger.LogError("Password is not specified for File Storage {fileStoragePath}", fileStoragePath);
            return false;
        }


        if (_par.FileStorageForExchange.FtpSiteLsFileOffset == 0)
        {
            Logger.LogError("FtpSiteLsFileOffset is not specified for File Storage {fileStoragePath}", fileStoragePath);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_par.ServerInfo.ServerName))
        {
            StShared.WriteErrorLine(
                $"ServerName is not specified for server {_par.ServerInfo.GetItemKey()} and project {_par.ProjectName}",
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
            StShared.WriteErrorLine($"file name for Generate is not specified", true);
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

        if (string.IsNullOrWhiteSpace(_par.Project.ServiceName))
        {
            StShared.WriteErrorLine($"Project.ServiceName is not specified for server {_par.ProjectName}", true);
            return false;
        }

        var sf = new FileInfo(_par.ServerInfo.AppSettingsEncodedJsonFileName);


        var createInstallScript = new CreateServiceInstallScript(Logger, UseConsole, scriptFileNameForSave,
            _par.ServerInfo.ServerSidePort, ftpSiteAddress, userName, password, startPath, _par.ProjectName,
            serverData.Runtime, _par.ServerInfo.EnvironmentName, serverData.ServerSideDownloadFolder,
            serverData.ServerSideDeployFolder, _par.Project.ServiceName, sf.Name,
            serverData.FilesUserName, _par.FileStorageForExchange.FtpSiteLsFileOffset);
        return createInstallScript.Run();
    }
}