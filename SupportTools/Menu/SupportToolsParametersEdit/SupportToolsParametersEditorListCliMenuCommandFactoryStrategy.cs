using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.SupportToolsParametersEdit;

// ReSharper disable once ClassNeverInstantiated.Global
public class SupportToolsParametersEditorListCliMenuCommandFactoryStrategy(
    IApplication application,
    ILogger<SupportToolsParametersEditorListCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        var supportToolsParametersEditor =
            new SupportToolsParametersEditor(application, logger, httpClientFactory, parameters, parametersManager);
        return new ParametersEditorListCliMenuCommand(supportToolsParametersEditor);
    }
}
