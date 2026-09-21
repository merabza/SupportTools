using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;

// ReSharper disable once ClassNeverInstantiated.Global
public class SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    ILogger<SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SaveEditorConfigAsNewTemplateCliMenuCommand(logger, parametersManager, menuParameters.ProjectName);
    }
}
