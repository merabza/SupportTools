using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.CreateProject;
using SystemTools.SystemToolsShared;

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
