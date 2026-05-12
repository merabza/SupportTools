using System;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CreateNewServerInfo;

// ReSharper disable once ClassNeverInstantiated.Global
public class CreateNewServerInfoFactoryStrategy(
    ILogger<CreateNewServerInfoFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IApplication application,
    IServiceProvider serviceProvider,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var serverInfoCruder = ServerInfoCruder.Create(application, serviceProvider, logger, httpClientFactory,
            parametersManager, menuParameters.ProjectName);

        //ახალი სერვერის ინფორმაციის შექმნა
        return new NewItemCliMenuCommand(serverInfoCruder, serverInfoCruder.CrudNamePlural,
            $"New {serverInfoCruder.CrudName}");
    }
}
