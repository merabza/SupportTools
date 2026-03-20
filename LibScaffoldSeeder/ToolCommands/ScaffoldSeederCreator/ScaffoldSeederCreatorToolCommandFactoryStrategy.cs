using System.Net.Http;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using Serilog.Core;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands.ScaffoldSeederCreator;

// ReSharper disable once UnusedType.Global
public class ScaffoldSeederCreatorToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<ScaffoldSeederCreatorToolCommandFactoryStrategy> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ScaffoldSeederCreatorToolCommandFactoryStrategy(ILogger<ScaffoldSeederCreatorToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.ScaffoldSeederCreator);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        //სკაფოლდინგისა და სიდინგის პროექტების შექმნა
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var scaffoldSeederCreatorParameters =
            ScaffoldSeederCreatorParameters.Create(_logger, supportToolsParameters, projectName, true);
        if (scaffoldSeederCreatorParameters is not null)
        {
            return new ScaffoldSeederCreatorToolCommand(_logger, _httpClientFactory, true,
                scaffoldSeederCreatorParameters, parametersManager);
        }

        StShared.WriteErrorLine("scaffoldSeederCreatorParameters is null", true);
        return null;
    }
}
