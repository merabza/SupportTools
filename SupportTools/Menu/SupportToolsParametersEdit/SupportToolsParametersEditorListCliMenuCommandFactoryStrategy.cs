using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.SupportToolsParametersEdit;

// ReSharper disable once ClassNeverInstantiated.Global
public class SupportToolsParametersEditorListCliMenuCommandFactoryStrategy(
    ILogger<SupportToolsParametersEditorListCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        var supportToolsParametersEditor =
            new SupportToolsParametersEditor(logger, httpClientFactory, parameters, parametersManager);
        return new ParametersEditorListCliMenuCommand(supportToolsParametersEditor);
    }
}
