using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.AppSettingsPreparer;

// ReSharper disable once UnusedType.Global
public class ApplicationSettingsPreparerToolCommandFactoryStrategy(
    ILogger<ApplicationSettingsPreparerToolCommandFactoryStrategy> logger) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectServerTools.AppSettingsPreparer);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var appSettingsPreparerParameters = AppSettingsPreparerParameters.Create(supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName, projectToolsFactoryStrategyParameters.ServerInfo);
        if (appSettingsPreparerParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new ApplicationSettingsPreparerToolCommand(logger, appSettingsPreparerParameters, parametersManager));
        }

        StShared.WriteErrorLine("appSettingsPreparerParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
