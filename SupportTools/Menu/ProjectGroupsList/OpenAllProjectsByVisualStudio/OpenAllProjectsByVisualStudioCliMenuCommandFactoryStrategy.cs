using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.OpenAllProjectsByVisualStudio;

// ReSharper disable once ClassNeverInstantiated.Global
public class OpenAllProjectsByVisualStudioCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    ILogger<OpenAllProjectsByVisualStudioCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new OpenAllProjectsByVisualStudioCliMenuCommand(parametersManager, menuParameters.ProjectGroupName,
            logger);
    }
}
