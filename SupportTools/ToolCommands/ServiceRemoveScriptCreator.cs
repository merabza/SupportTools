using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibMenuInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ToolActions;
using SupportTools.ToolCommandParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.ToolCommands;

public class ServiceRemoveScriptCreator : ToolCommand
{
    private const string ActionName = "Creating Service Remove Script";
    private const string ActionDescription = "Creating Service Remove Script";
    private readonly ILogger _logger;
    private readonly ServiceRemoveScriptCreatorParameters _par;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServiceRemoveScriptCreator(ILogger logger, ServiceRemoveScriptCreatorParameters par,
        IParametersManager? parametersManager) : base(logger, ActionName, par, parametersManager, ActionDescription)
    {
        _logger = logger;
        _par = par;
    }


    protected override async Task<bool> RunAction(CancellationToken cancellationToken = default)
    {
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

        var securityFolder = _par.SecurityFolder;
        string? defCloneFile = null;
        if (securityFolder is not null)
            defCloneFile = Path.Combine(securityFolder, _par.ProjectName, _par.ServerInfo.ServerName,
                _par.ServerInfo.EnvironmentName, $"{_par.ProjectName}Remove.sh");
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

        if (string.IsNullOrWhiteSpace(serverData.ServerSideDeployFolder))
        {
            StShared.WriteErrorLine(
                $"serverData.ServerSideDeployFolder is not specified for server {_par.ServerInfo.ServerName}", true);
            return false;
        }

        var createRemoveScript = new CreateServiceRemoveScript(_logger, scriptFileNameForSave, _par.ProjectName,
            _par.ServerInfo.EnvironmentName, serverData.ServerSideDeployFolder);
        return await createRemoveScript.Run(cancellationToken);
    }
}