using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibCodeGenerator.ToolCommands.GenerateApiRoutes;

public class GenerateApiRoutesToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<GenerateApiRoutesToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateApiRoutesToolCommandFactoryStrategy(ILogger<GenerateApiRoutesToolCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.GenerateApiRoutes);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        //სკაფოლდინგისა და სიდინგის პროექტების შექმნა
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var generateApiRoutesParameters = GenerateApiRoutesToolParameters.Create(supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (generateApiRoutesParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new GenerateApiRoutesToolCommand(_logger, parametersManager, generateApiRoutesParameters));
        }

        StShared.WriteErrorLine("generateApiRoutesParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
