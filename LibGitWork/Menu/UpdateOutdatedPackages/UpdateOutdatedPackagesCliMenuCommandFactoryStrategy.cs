using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibGitWork.Menu.UpdateOutdatedPackages;

// ReSharper disable once UnusedType.Global
public class UpdateOutdatedPackagesCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<UpdateOutdatedPackagesCliMenuCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOutdatedPackagesCliMenuCommandFactoryStrategy(
        ILogger<UpdateOutdatedPackagesCliMenuCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string MenuCommandName => UpdateOutdatedPackagesCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new UpdateOutdatedPackagesCliMenuCommand(_logger, parametersManager1);
    }
}
