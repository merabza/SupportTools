using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ImportProject;

// ReSharper disable once UnusedType.Global
public class ImportProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    public string MenuCommandName => ImportProjectCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new ImportProjectCliMenuCommand(parametersManager);
    }
}
