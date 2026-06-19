using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsBuild;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckGroupProjectsBuildCliMenuCommandFactoryStrategy(
    IApplication app,
    ILogger<CheckGroupProjectsBuildCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckGroupProjectsBuildCliMenuCommand(logger, app.AppName, (ParametersManager)parametersManager,
            menuParameters, menuParameters.ProjectGroupName);
    }
}
