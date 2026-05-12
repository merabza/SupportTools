using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.EditItemAllFieldsInSequence;

// ReSharper disable once ClassNeverInstantiated.Global
public class EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    IApplication application,
    ILogger<EditItemAllFieldsInSequenceCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var projectCruder = ProjectCruder.Create(application, logger, httpClientFactory, parametersManager);
        return new EditItemAllFieldsInSequenceCliMenuCommand(projectCruder, menuParameters.ProjectName);
    }
}
