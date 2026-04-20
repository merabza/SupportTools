using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;

// ReSharper disable once UnusedType.Global
public class SelectProjectAllowToolsFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SelectProjectAllowToolsFactoryStrategy(ILogger<SelectProjectAllowToolsFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, MenuParameters menuParameters, IParametersManager parametersManager,
        IApplication application)
    {
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public string MenuCommandName => nameof(SelectProjectAllowToolsCliMenuCommand);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new SelectProjectAllowToolsCliMenuCommand(_parametersManager, _menuParameters.ProjectName);
    }
}
