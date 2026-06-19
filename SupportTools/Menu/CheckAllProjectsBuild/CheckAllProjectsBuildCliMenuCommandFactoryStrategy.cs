using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.CheckAllProjectsBuild;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckAllProjectsBuildCliMenuCommandFactoryStrategy(
    ILogger<CheckAllProjectsBuildCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new CheckAllProjectsBuildCliMenuCommand(logger, parametersManager1, menuParameters);
    }
}
