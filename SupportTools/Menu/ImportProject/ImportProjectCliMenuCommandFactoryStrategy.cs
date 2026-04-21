using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ImportProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ImportProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ImportProjectCliMenuCommandFactoryStrategy(IParametersManager parametersManager)
    {
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new ImportProjectCliMenuCommand(_parametersManager);
    }
}
