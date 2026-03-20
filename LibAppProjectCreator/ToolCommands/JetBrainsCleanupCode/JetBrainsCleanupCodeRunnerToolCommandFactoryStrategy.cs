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

    public JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy(
        ILogger<JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.JetBrainsCleanupCode);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //სოლუშენის ფაილის მითითებით კოდის გასაწმენდად და მოსაწესრიგებლად
        var jetBrainsCleanupCodeRunnerParameters =
            JetBrainsCleanupCodeRunnerParameters.Create(supportToolsParameters, projectName);
        if (jetBrainsCleanupCodeRunnerParameters is not null)
        {
            return new JetBrainsCleanupCodeRunnerToolCommand(_logger, jetBrainsCleanupCodeRunnerParameters);
        }

        StShared.WriteErrorLine("dataSeederParameters is null", true);
        return null;
    }
}
