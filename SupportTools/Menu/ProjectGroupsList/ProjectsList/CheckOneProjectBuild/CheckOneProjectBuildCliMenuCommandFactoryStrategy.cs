using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectBuild;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckOneProjectBuildCliMenuCommandFactoryStrategy(
    IApplication app,
    ILogger<CheckOneProjectBuildCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckOneProjectBuildCliMenuCommand(logger, app.AppName, (ParametersManager)parametersManager,
            menuParameters, menuParameters.ProjectName);
    }
}
