using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppProjectCreator.ToolCommands.JetBrainsCleanupCode;

public class JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy(
        ILogger<JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.JetBrainsCleanupCode);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //სოლუშენის ფაილის მითითებით კოდის გასაწმენდად და მოსაწესრიგებლად
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;
        var jetBrainsCleanupCodeRunnerParameters = JetBrainsCleanupCodeRunnerParameters.Create(supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (jetBrainsCleanupCodeRunnerParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new JetBrainsCleanupCodeRunnerToolCommand(_logger, jetBrainsCleanupCodeRunnerParameters));
        }

        StShared.WriteErrorLine("dataSeederParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
