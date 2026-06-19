using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.CheckAllProjectsBuild;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckAllProjectsBuildCliMenuCommandFactoryStrategy(
    IApplication app,
    ILogger<CheckAllProjectsBuildCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new CheckAllProjectsBuildCliMenuCommand(logger, app.AppName, parametersManager1, menuParameters);
    }
}
