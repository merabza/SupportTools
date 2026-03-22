using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ServiceRemoveScriptCreator;

// ReSharper disable once UnusedType.Global
public class ServiceRemoveScriptCreatorToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<ServiceRemoveScriptCreatorToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServiceRemoveScriptCreatorToolCommandFactoryStrategy(
        ILogger<ServiceRemoveScriptCreatorToolCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.CorrectNewDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        var serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var serviceRemoveScriptCreatorParameters =
            ServiceRemoveScriptCreatorParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (serviceRemoveScriptCreatorParameters is not null)
        {
            return new ValueTask<IToolCommand?>(new ServiceRemoveScriptCreatorToolCommand(_logger,
                serviceRemoveScriptCreatorParameters, parametersManager));
        }

        StShared.WriteErrorLine("ServiceRemoveScriptCreatorParameters is not created", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
