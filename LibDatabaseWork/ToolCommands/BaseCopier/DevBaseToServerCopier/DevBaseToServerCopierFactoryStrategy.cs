using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.BaseCopier.DevBaseToServerCopier;

public class DevBaseToServerCopierFactoryStrategy : BaseCopierFactoryStrategy, IToolCommandFactoryStrategy
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DevBaseToServerCopierFactoryStrategy(ILogger<DevBaseToServerCopierFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    public string ToolCommandName => nameof(EProjectTools.JsonFromProjectDbProjectGetter);

    public async ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

        if (project.DevDatabaseParameters == null)
        {
            StShared.WriteErrorLine($"DevDatabaseParameters is not specified for Project with name {projectName}",
                true);
            return null;
        }

        if (serverInfo.NewDatabaseParameters != null)
        {
            return await CreateToolCommand(parametersManager, project.DevDatabaseParameters,
                serverInfo.NewDatabaseParameters, cancellationToken);
        }

        StShared.WriteErrorLine($"NewDatabaseParameters is not specified {serverInfo.ServerName}", true);
        return null;
    }
}
