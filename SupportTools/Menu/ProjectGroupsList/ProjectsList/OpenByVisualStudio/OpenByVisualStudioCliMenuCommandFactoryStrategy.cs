using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;

// ReSharper disable once UnusedType.Global
public class OpenByVisualStudioCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<OpenByVisualStudioCliMenuCommandFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public OpenByVisualStudioCliMenuCommandFactoryStrategy(MenuParameters menuParameters,
        ILogger<OpenByVisualStudioCliMenuCommandFactoryStrategy> logger)
    {
        _menuParameters = menuParameters;
        _logger = logger;
    }

    public string MenuCommandName => OpenByVisualStudioCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new OpenByVisualStudioCliMenuCommand(parametersManager, _menuParameters.ProjectName, _logger);
    }
}
