using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.UpdateOutdatedPackages;

// ReSharper disable once ClassNeverInstantiated.Global
public class UpdateOutdatedPackagesCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<UpdateOutdatedPackagesCliMenuCommandFactoryStrategy> _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOutdatedPackagesCliMenuCommandFactoryStrategy(
        ILogger<UpdateOutdatedPackagesCliMenuCommandFactoryStrategy> logger, IParametersManager parametersManager)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)_parametersManager;
        return new UpdateOutdatedPackagesCliMenuCommand(_logger, parametersManager1);
    }
}
