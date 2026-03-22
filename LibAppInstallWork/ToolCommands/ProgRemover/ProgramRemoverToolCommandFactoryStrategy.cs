using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ProgRemover;

// ReSharper disable once UnusedType.Global
public class ProgramRemoverToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProgramRemoverToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramRemoverToolCommandFactoryStrategy(ILogger<ProgramRemoverToolCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.CorrectNewDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        var projectName = projectToolsFactoryStrategyParameters.ProjectName;
        var serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //  Remove, //პროგრამის წაშლა
        var serviceStartStopParameters =
            ProgramRemoverParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (serviceStartStopParameters is not null)
        {
            return new ValueTask<IToolCommand?>(new ProgramRemoverToolCommand(_logger, _httpClientFactory,
                serviceStartStopParameters, parametersManager, true));
        }

        StShared.WriteErrorLine("serviceStartStopParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
