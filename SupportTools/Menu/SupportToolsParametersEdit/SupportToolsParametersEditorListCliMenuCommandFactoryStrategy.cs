using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.SupportToolsParametersEdit;

// ReSharper disable once ClassNeverInstantiated.Global
public class SupportToolsParametersEditorListCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SupportToolsParametersEditorListCliMenuCommandFactoryStrategy> _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsParametersEditorListCliMenuCommandFactoryStrategy(
        ILogger<SupportToolsParametersEditorListCliMenuCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var supportToolsParametersEditor =
            new SupportToolsParametersEditor(_logger, _httpClientFactory, parameters, _parametersManager);
        return new ParametersEditorListCliMenuCommand(supportToolsParametersEditor);
    }
}
