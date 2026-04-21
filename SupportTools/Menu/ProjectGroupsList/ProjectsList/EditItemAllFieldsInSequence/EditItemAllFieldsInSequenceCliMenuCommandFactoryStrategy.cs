using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.EditItemAllFieldsInSequence;

// ReSharper disable once UnusedType.Global
public class EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IApplication _application;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy(MenuParameters menuParameters,
        IApplication application, ILogger<EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _application = application;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var projectCruder = ProjectCruder.Create(_application.AppName, _logger, _httpClientFactory, _parametersManager);
        return new EditItemAllFieldsInSequenceCliMenuCommand(projectCruder, _menuParameters.ProjectName);
    }
}
