using System.Collections.Generic;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectParametersList;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectParametersListFactoryStrategy(
    ILogger<ProjectParametersListFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager,
    IApplication application) : IMenuCommandListFactoryStrategy
{
    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var projectCruder = ProjectCruder.Create(application, logger, httpClientFactory, parametersManager);

        return projectCruder.GetDetailsSubMenu(menuParameters.ProjectName);
    }
}
