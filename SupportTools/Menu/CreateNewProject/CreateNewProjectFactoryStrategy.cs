using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.CreateNewProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class CreateNewProjectFactoryStrategy(
    ILogger<CreateNewProjectFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IApplication application,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        var projectCruder = new ProjectCruder(application, logger, httpClientFactory, parametersManager,
            parameters.Projects);

        //ახალი პროექტის შექმნა
        return new NewItemCliMenuCommand(projectCruder, projectCruder.CrudNamePlural, $"New {projectCruder.CrudName}");
    }
}
