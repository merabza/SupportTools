using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ImportProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ImportProjectCliMenuCommandFactoryStrategy(IParametersManager parametersManager)
    : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new ImportProjectCliMenuCommand(parametersManager);
    }
}
