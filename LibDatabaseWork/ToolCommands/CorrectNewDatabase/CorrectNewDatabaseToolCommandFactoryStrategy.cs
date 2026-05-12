using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.CorrectNewDatabase;

public class CorrectNewDatabaseToolCommandFactoryStrategy(ILogger<CorrectNewDatabaseToolCommandFactoryStrategy> logger)
    : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectTools.CorrectNewDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var correctNewDbParameters = CorrectNewDbParameters.Create(logger, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (correctNewDbParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new CorrectNewDatabaseToolCommand(logger, correctNewDbParameters, parametersManager)); //ახალი ბაზის 
        }

        StShared.WriteErrorLine("correctNewDbParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
