using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.BaseCopier.ServerBaseToProdCopyCopier;

public class ServerBaseToProdCopyCopierFactoryStrategy : BaseCopierFactoryStrategy, IToolCommandFactoryStrategy
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerBaseToProdCopyCopierFactoryStrategy(ILogger<ServerBaseToProdCopyCopierFactoryStrategy> logger,
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

        if (project.ProdCopyDatabaseParameters == null)
        {
            StShared.WriteErrorLine($"ProdCopyDatabaseParameters is not specified for Project with name {projectName}",
                true);
            return null;
        }

        if (serverInfo.CurrentDatabaseParameters != null)
        {
            return await CreateToolCommand(parametersManager, serverInfo.CurrentDatabaseParameters,
                project.ProdCopyDatabaseParameters, cancellationToken);
        }

        StShared.WriteErrorLine($"CurrentDatabaseParameters is not specified {serverInfo.ServerName}", true);
        return null;
    }
}
