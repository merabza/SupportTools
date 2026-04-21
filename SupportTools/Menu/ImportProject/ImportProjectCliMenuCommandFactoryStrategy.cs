using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ImportProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ImportProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IParametersManager _parametersManager;

    public ImportProjectCliMenuCommandFactoryStrategy(IParametersManager parametersManager)
    {
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(ImportProjectCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand()
    {
        return new ImportProjectCliMenuCommand(_parametersManager);
    }
}
