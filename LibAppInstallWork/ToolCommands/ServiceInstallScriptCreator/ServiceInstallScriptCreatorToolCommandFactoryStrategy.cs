using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ServiceInstallScriptCreator;

// ReSharper disable once UnusedType.Global
public class ServiceInstallScriptCreatorToolCommandFactoryStrategy(
    ILogger<ServiceInstallScriptCreatorToolCommandFactoryStrategy> logger) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectServerTools.ServiceInstallScriptCreator);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var serviceInstallScriptCreatorParameters = ServiceInstallScriptCreatorParameters.Create(supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName, projectToolsFactoryStrategyParameters.ServerInfo);

        if (serviceInstallScriptCreatorParameters is null)
        {
            StShared.WriteErrorLine("serviceInstallScriptCreatorParameters is null", true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        //var appSettingsEncoderParameters = AppSettingsEncoderParameters.Create(supportToolsParameters,
        //    projectToolsFactoryStrategyParameters.ProjectName, projectToolsFactoryStrategyParameters.ServerInfo);
        //if (appSettingsEncoderParameters is not null)
        //{
        return ValueTask.FromResult<IToolCommand?>(new ServiceInstallScriptCreatorToolCommand(logger,
            serviceInstallScriptCreatorParameters, parametersManager));
        //}

        //StShared.WriteErrorLine("appSettingsEncoderParameters is null", true);
        //return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
