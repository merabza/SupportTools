using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsBuild;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckGroupProjectsBuildCliMenuCommandFactoryStrategy(
    ILogger<CheckGroupProjectsBuildCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckGroupProjectsBuildCliMenuCommand(logger, (ParametersManager)parametersManager, menuParameters,
            menuParameters.ProjectGroupName);
    }
}
