using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

// ReSharper disable once UnusedType.Global
public class TransferProdCopyToDevByPairsToolCommandFactoryStrategy(
    ILogger<TransferProdCopyToDevByPairsToolCommandFactoryStrategy> logger) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectTools.TransferProdCopyToDevByPairs);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var parameters = TransferProdCopyToDevByPairsParameters.Create(logger, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName);
        if (parameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new TransferProdCopyToDevByPairsToolCommand(logger, parameters, parametersManager));
        }

        StShared.WriteErrorLine("TransferProdCopyToDevByPairsParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
