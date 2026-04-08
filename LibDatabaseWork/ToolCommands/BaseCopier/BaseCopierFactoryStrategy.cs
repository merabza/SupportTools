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
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    protected BaseCopierFactoryStrategy(string appName, ILogger logger, IHttpClientFactory httpClientFactory)
    {
        _appName = appName;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected async ValueTask<IToolCommand> CreateToolCommand(IParametersManager parametersManager,
        DatabaseParameters fromDatabaseParameters, DatabaseParameters toDatabaseParameters,
        CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

        //if (project.DevDatabaseParameters == null)
        //{
        //    StShared.WriteErrorLine($"DevDatabaseParameters is not specified for Project with name {projectName}",
        //        true);
        //    return null;
        //}

        //if (serverInfo.NewDatabaseParameters == null)
        //{
        //    StShared.WriteErrorLine($"NewDatabaseParameters is not specified {serverInfo.ServerName}", true);
        //    return null;
        //}

        CopyBaseParameters? copyBaseParametersDevToProd =
            await CopyBaseParametersFactory.CreateCopyBaseParameters(_appName, _logger, _httpClientFactory,
                fromDatabaseParameters, toDatabaseParameters, supportToolsParameters, cancellationToken);
        if (copyBaseParametersDevToProd is not null)
        {
            return new BaseCopierToolCommand(_logger, copyBaseParametersDevToProd, parametersManager);
        }

        StShared.WriteErrorLine("copyBaseParametersDevToProd is null", true);
        return null;
    }
}
