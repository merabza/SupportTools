using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.BaseCopier;

public class BaseCopierFactoryStrategy
{
    private readonly IApplication _app;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    protected BaseCopierFactoryStrategy(IApplication app, ILogger logger, IHttpClientFactory httpClientFactory)
    {
        _app = app;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected async ValueTask<IToolCommand> CreateToolCommand(IParametersManager parametersManager,
        DatabaseParameters fromDatabaseParameters, DatabaseParameters toDatabaseParameters,
        CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        CopyBaseParameters? copyBaseParametersDevToProd =
            await CopyBaseParametersFactory.CreateCopyBaseParameters(_app.Name, _logger, _httpClientFactory,
                fromDatabaseParameters, toDatabaseParameters, supportToolsParameters, cancellationToken);
        if (copyBaseParametersDevToProd is not null)
        {
            return new BaseCopierToolCommand(_logger, copyBaseParametersDevToProd, parametersManager);
        }

        StShared.WriteErrorLine("copyBaseParametersDevToProd is null", true);
        return null;
    }
}
