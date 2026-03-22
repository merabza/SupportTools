using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliParametersApiClientsEdit.FieldEditors;
using AppCliTools.CliParametersDataEdit.FieldEditors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.FieldEditors;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ServerInfoCruder : ParCruder<ServerInfoModel>
{
    private readonly string _projectName;
    private readonly ServiceProvider _serviceProvider;

    public ServerInfoCruder(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager,
        Dictionary<string, ServerInfoModel> currentValuesDictionary, string projectName,
        ServiceProvider serviceProvider) : base(parametersManager, currentValuesDictionary, "Server", "Servers", true)
    {
        _projectName = projectName;
        _serviceProvider = serviceProvider;

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        ProjectModel project =
            parameters.GetProject(_projectName) ?? throw new ArgumentNullException(nameof(projectName));

        FieldEditors.Add(new ServerDataNameFieldEditor(logger, httpClientFactory, nameof(ServerInfoModel.ServerName),
            ParametersManager));
        FieldEditors.Add(new EnvironmentNameFieldEditor(nameof(ServerInfoModel.EnvironmentName), ParametersManager));
        if (project.IsService)
        {
            FieldEditors.Add(new ApiClientNameFieldEditor(nameof(ServerInfoModel.WebAgentNameForCheck), logger,
                httpClientFactory, ParametersManager));
            FieldEditors.Add(new IntFieldEditor(nameof(ServerInfoModel.ServerSidePort)));
            FieldEditors.Add(new TextFieldEditor(nameof(ServerInfoModel.ApiVersionId)));
            FieldEditors.Add(new TextFieldEditor(nameof(ServerInfoModel.ServiceUserName)));
        }

        FieldEditors.Add(new FilePathFieldEditor(nameof(ServerInfoModel.AppSettingsJsonSourceFileName)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ServerInfoModel.AppSettingsEncodedJsonFileName)));
        //FieldEditors.Add(new DatabasesExchangeParametersFieldEditor(_logger, httpClientFactory,
        //    nameof(ServerInfoModel.DatabasesExchangeParameters), parametersManager));
        FieldEditors.Add(new DatabaseParametersFieldEditor(logger, httpClientFactory,
            nameof(ServerInfoModel.CurrentDatabaseParameters), parametersManager));
        FieldEditors.Add(new DatabaseParametersFieldEditor(logger, httpClientFactory,
            nameof(ServerInfoModel.NewDatabaseParameters), parametersManager));
    }

    public static ServerInfoCruder Create(ServiceProvider serviceProvider, ILogger logger,
        IHttpClientFactory httpClientFactory, ParametersManager parametersManager, string projectName)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        Dictionary<string, ServerInfoModel> currentValuesDictionary = parameters.GetProject(projectName)?.ServerInfos ??
                                                                      throw new ArgumentNullException(
                                                                          nameof(projectName));
        return new ServerInfoCruder(logger, httpClientFactory, parametersManager, currentValuesDictionary, projectName,
            serviceProvider);
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        //დასაშვები ინსტრუმენტების არჩევა
        itemSubMenuSet.AddMenuItem(new SelectServerAllowToolsCliMenuCommand(ParametersManager, _projectName, itemName));
        //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        ServerInfoModel? server = parameters.GetServerByProject(_projectName, itemName);
        ProjectModel? project = parameters.GetProject(_projectName);

        if (server == null || project == null)
        {
            return;
        }

        foreach (EProjectServerTools tool in Enum.GetValues<EProjectServerTools>()
                     .Intersect(server.AllowToolsList ?? []))
        {
            itemSubMenuSet.AddMenuItem(new ProjectServerToolTaskCliMenuCommand(tool, _projectName, server,
                ParametersManager, _serviceProvider));
        }
    }
}
