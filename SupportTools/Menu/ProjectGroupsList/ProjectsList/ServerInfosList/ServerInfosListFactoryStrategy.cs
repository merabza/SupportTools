using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ServerInfosList;

// ReSharper disable once ClassNeverInstantiated.Global
public class ServerInfosListFactoryStrategy(
    IApplication app,
    IServiceProvider serviceProvider,
    ILogger<ServerInfosListFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandListFactoryStrategy
{
    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(menuParameters.ProjectName);

        if (project?.ServerInfos != null)
        {
            return
            [
                .. project.ServerInfos.OrderBy(o => o.Value.GetItemKey()).Select(kvp =>
                    new ServerInfoSubMenuCliMenuCommand(app, logger, httpClientFactory, kvp.Value.GetItemKey(),
                        parametersManager, menuParameters.ProjectName, kvp.Key, serviceProvider))
            ];
        }

        return [];
    }
}
