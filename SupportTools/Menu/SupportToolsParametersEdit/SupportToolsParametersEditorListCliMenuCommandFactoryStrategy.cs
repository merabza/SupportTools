using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.UpdateOutdatedPackages;
using SupportToolsData.Models;

namespace SupportTools.Menu.SupportToolsParametersEdit;

// ReSharper disable once UnusedType.Global
public class SupportToolsParametersEditorListCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SupportToolsParametersEditorListCliMenuCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsParametersEditorListCliMenuCommandFactoryStrategy(
        ILogger<SupportToolsParametersEditorListCliMenuCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string StrategyName => nameof(SupportToolsParametersEditorListCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        var supportToolsParametersEditor =
            new SupportToolsParametersEditor(_logger, _httpClientFactory, parameters, parametersManager);
        return new ParametersEditorListCliMenuCommand(supportToolsParametersEditor);
    }
}
