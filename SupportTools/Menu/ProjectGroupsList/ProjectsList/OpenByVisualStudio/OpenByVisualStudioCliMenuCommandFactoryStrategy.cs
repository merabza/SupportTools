using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;

// ReSharper disable once ClassNeverInstantiated.Global
public class OpenByVisualStudioCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<OpenByVisualStudioCliMenuCommandFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public OpenByVisualStudioCliMenuCommandFactoryStrategy(MenuParameters menuParameters,
        ILogger<OpenByVisualStudioCliMenuCommandFactoryStrategy> logger, IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new OpenByVisualStudioCliMenuCommand(_parametersManager, _menuParameters.ProjectName, _logger);
    }
}
