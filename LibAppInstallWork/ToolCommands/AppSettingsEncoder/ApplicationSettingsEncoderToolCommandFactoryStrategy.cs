using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.AppSettingsEncoder;

public class ApplicationSettingsEncoderToolCommandFactoryStrategy(
    ILogger<ApplicationSettingsEncoderToolCommandFactoryStrategy> logger) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectServerTools.AppSettingsEncoder);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var appSettingsEncoderParameters = AppSettingsEncoderParameters.Create(supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName, projectToolsFactoryStrategyParameters.ServerInfo);
        if (appSettingsEncoderParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new ApplicationSettingsEncoderToolCommand(logger, appSettingsEncoderParameters, parametersManager));
        }

        StShared.WriteErrorLine("appSettingsEncoderParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
