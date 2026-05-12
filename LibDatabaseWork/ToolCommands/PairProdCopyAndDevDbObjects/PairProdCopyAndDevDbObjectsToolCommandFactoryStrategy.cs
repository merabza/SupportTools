using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

// ReSharper disable once UnusedType.Global
public class PairProdCopyAndDevDbObjectsToolCommandFactoryStrategy(
    ILogger<PairProdCopyAndDevDbObjectsToolCommandFactoryStrategy> logger) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectTools.PairProdCopyAndDevDbObjects);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var pairDbObjectsParameters = PairDbObjectsParameters.Create(logger, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (pairDbObjectsParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new PairProdCopyAndDevDbObjectsToolCommand(logger, pairDbObjectsParameters, parametersManager));
        }

        StShared.WriteErrorLine("pairDbObjectsParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
