using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsEdit.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ServerInfoCruder : ParCruder
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _projectName;

    public ServerInfoCruder(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager,
        string projectName) : base(parametersManager, "Server", "Servers", true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _projectName = projectName;

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);

        if (project is null)
            throw new ArgumentNullException(nameof(project));


        FieldEditors.Add(new ServerDataNameFieldEditor(logger, httpClientFactory, nameof(ServerInfoModel.ServerName),
            ParametersManager));
        FieldEditors.Add(new EnvironmentNameFieldEditor(nameof(ServerInfoModel.EnvironmentName), ParametersManager));
        if (project.IsService)
        {
            FieldEditors.Add(new ApiClientNameFieldEditor(logger, httpClientFactory,
                nameof(ServerInfoModel.WebAgentNameForCheck), ParametersManager));
            FieldEditors.Add(new IntFieldEditor(nameof(ServerInfoModel.ServerSidePort)));
            FieldEditors.Add(new TextFieldEditor(nameof(ServerInfoModel.ApiVersionId)));
            FieldEditors.Add(new TextFieldEditor(nameof(ServerInfoModel.ServiceUserName)));
        }

        FieldEditors.Add(new FilePathFieldEditor(nameof(ServerInfoModel.AppSettingsJsonSourceFileName)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ServerInfoModel.AppSettingsEncodedJsonFileName)));
        FieldEditors.Add(new DatabasesExchangeParametersFieldEditor(_logger, httpClientFactory,
            nameof(ServerInfoModel.DatabasesExchangeParameters), parametersManager));
    }

    private Dictionary<string, ServerInfoModel> GetServerInfos()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);
        return project?.ServerInfos ?? [];
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetServerInfos().ToDictionary(p => p.Key, p => (ItemData)p.Value);
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var itemDataDict = GetCrudersDictionary();

        return itemDataDict.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not ServerInfoModel newServer)
            throw new Exception("newServer is null in ServerInfoCruder.UpdateRecordWithKey");

        var crudersDictionary = GetCrudersDictionary() ??
                                throw new Exception(
                                    "crudersDictionary is null in ServerInfoCruder.UpdateRecordWithKey");
        crudersDictionary[recordKey] = newServer;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not ServerInfoModel newServer)
            throw new Exception("newServer is null in ServerInfoCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName) ??
                      throw new Exception($"Project with name {_projectName} not found");
        project.ServerInfos.Add(recordKey, newServer);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName) ??
                      throw new Exception($"Project with name {_projectName} not found");
        project.ServerInfos.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(ItemData? defaultItemData)
    {
        return new ServerInfoModel();
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);


        //დასაშვები ინსტრუმენტების არჩევა
        itemSubMenuSet.AddMenuItem(new SelectServerAllowToolsCliMenuCommand(ParametersManager, _projectName, recordKey),
            "Select Allow tools...");

        //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var server = parameters.GetServerByProject(_projectName, recordKey);
        var project = parameters.GetProject(_projectName);

        if (server == null || project == null)
            return;
        foreach (var tool in ToolCommandFabric.ToolsByProjectsAndServers.Intersect(server.AllowToolsList ?? []))
            itemSubMenuSet.AddMenuItem(
                new ToolTaskCliMenuCommand(_logger, _httpClientFactory, tool, _projectName, server, ParametersManager),
                tool.ToString());
    }
}