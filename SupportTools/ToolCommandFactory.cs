using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools;

public static class ToolCommandFactory
{
    public static async ValueTask<IToolCommand?> CreateProjectToolCommand(EProjectTools tool,
        ServiceProvider serviceProvider, IParametersManager parametersManager, string projectName)
    {
        Dictionary<string, IToolCommandFactoryStrategy>? toolCommandStrategies = serviceProvider
            .GetService<IEnumerable<IToolCommandFactoryStrategy>>()?.ToDictionary(s => s.ToolCommandName, s => s);

        if (toolCommandStrategies == null)
        {
            StShared.WriteErrorLine("No IToolCommandFactoryStrategy implementations found", true);
            return null;
        }

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return null;
        }

        if (toolCommandStrategies.TryGetValue(tool.ToString(), out IToolCommandFactoryStrategy? strategy))
        {
            return await strategy.CreateToolCommand(parametersManager,
                new ProjectToolsFactoryStrategyParameters { ProjectName = projectName });
        }

        StShared.WriteErrorLine($"No strategy found for tool {tool}", true);
        return null;
    }

    public static async ValueTask<IToolCommand?> CreateProjectServerToolCommand(EProjectServerTools tool,
        ServiceProvider serviceProvider, IParametersManager parametersManager, string projectName,
        ServerInfoModel serverInfo, CancellationToken cancellationToken = default)
    {
        Dictionary<string, IToolCommandFactoryStrategy>? toolCommandStrategies = serviceProvider
            .GetService<IEnumerable<IToolCommandFactoryStrategy>>()?.ToDictionary(s => s.ToolCommandName, s => s);

        if (toolCommandStrategies == null)
        {
            StShared.WriteErrorLine("No IToolCommandFactoryStrategy implementations found", true);
            return null;
        }

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return null;
        }

        //შევამოწმოთ პროექტის პარამეტრები
        if (string.IsNullOrWhiteSpace(serverInfo.ServerName))
        {
            StShared.WriteErrorLine("Server name is not specified", true);
            return null;
        }

        //შევამოწმოთ სერვერის პარამეტრები
        ServerDataModel? server = supportToolsParameters.GetServerData(serverInfo.ServerName);
        if (server is null)
        {
            StShared.WriteErrorLine($"Server with name {serverInfo.ServerName} not found", true);
            return null;
        }

        if (toolCommandStrategies.TryGetValue(tool.ToString(), out IToolCommandFactoryStrategy? strategy))
        {
            return await strategy.CreateToolCommand(parametersManager,
                new ProjectServerToolsFactoryStrategyParameters { ProjectName = projectName, ServerInfo = serverInfo },
                cancellationToken);
        }

        StShared.WriteErrorLine($"No strategy found for tool {tool}", true);
        return null;
    }
}
