using CliParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Actions;
using SupportTools.ToolCommandParameters;

namespace SupportTools.ToolCommands;

public class InstallScriptCreator : ToolCommand
{
    private readonly InstallScriptCreatorParameters _par;
    private const string ActionName = "Creating Install Script";
    private const string ActionDescription = "Creating Install Script";

    //public InstallScriptCreator(ILogger logger, bool useConsole,
    //    ParametersManager parametersManager) : base(logger, useConsole, ActionName,
    //    parametersManager, ActionDescription)
    //{
    //}

    public InstallScriptCreator(ILogger logger, bool useConsole, InstallScriptCreatorParameters par,
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


        /*
         *
, string scriptFileName, 
        int portNumber,
        string ftpSiteAddress, 
        string ftpSiteUserName, 
        string ftpSitePassword, 
        string ftpSiteDirectory,
        string projectName, 
        string runTime, 
        string environmentName, 
        string serverSideDownloadFolder,
        string serverSideDeployFolder, 
        string serviceName, 
        string settingsFileName, 
        string serverSideServiceUserName,
        int ftpSiteLsFileOffset         *
         */


        var createInstallScript = new CreateInstallScript(Logger, UseConsole, _par.ScriptFileName,
            _par.ServerInfo.ServerSidePort, _par.FileStorageForExchange.FileStoragePath,
            _par.FileStorageForExchange.UserName, _par.FileStorageForExchange.Password, "/", _par.ProjectName, "",
            _par.ServerInfo.EnvironmentName, "", "", "", "", "", 72);
        return createInstallScript.Run();
    }
}