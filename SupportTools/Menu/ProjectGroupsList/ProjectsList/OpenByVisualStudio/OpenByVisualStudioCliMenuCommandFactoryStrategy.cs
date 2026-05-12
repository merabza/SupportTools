using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;

// ReSharper disable once ClassNeverInstantiated.Global
public class OpenByVisualStudioCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    ILogger<OpenByVisualStudioCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new OpenByVisualStudioCliMenuCommand(parametersManager, menuParameters.ProjectName, logger);
    }
}
