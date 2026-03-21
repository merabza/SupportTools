using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.CorrectNewDatabase;

public class CorrectNewDatabaseToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<CorrectNewDatabaseToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CorrectNewDatabaseToolCommandFactoryStrategy(ILogger<CorrectNewDatabaseToolCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.CorrectNewDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var correctNewDbParameters = CorrectNewDbParameters.Create(_logger, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (correctNewDbParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new CorrectNewDatabaseToolCommand(_logger, correctNewDbParameters, parametersManager)); //ახალი ბაზის 
        }

        StShared.WriteErrorLine("correctNewDbParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
