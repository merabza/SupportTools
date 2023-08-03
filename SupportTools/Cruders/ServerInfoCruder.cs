﻿using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsEdit.FieldEditors;
using Installer.AgentClients;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportTools.FieldEditors;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ServerInfoCruder : ParCruder
{
    private readonly ILogger _logger;
    private readonly string _projectName;

    public ServerInfoCruder(ILogger logger, ParametersManager parametersManager, string projectName) : base(
        parametersManager, "Server", "Servers")
    {
        _logger = logger;
        _projectName = projectName;

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);

        if (project is null)
            throw new ArgumentNullException(nameof(project));

        if (!project.IsService)
            return;

        WebAgentClientFabric webAgentClientFabric = new();
        FieldEditors.Add(new ApiClientNameFieldEditor(logger, nameof(ServerInfoModel.WebAgentNameForCheck),
            ParametersManager, webAgentClientFabric));
        FieldEditors.Add(new IntFieldEditor(nameof(ServerInfoModel.ServerSidePort)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInfoModel.ApiVersionId)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ServerInfoModel.AppSettingsJsonSourceFileName)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ServerInfoModel.AppSettingsEncodedJsonFileName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerInfoModel.ServiceUserName)));
        FieldEditors.Add(new DatabasesExchangeParametersFieldEditor(_logger,
            nameof(ServerInfoModel.DatabasesExchangeParameters), parametersManager));
    }

    private Dictionary<string, ServerInfoModel> GetServerInfos()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);
        return project?.ServerInfos ?? new Dictionary<string, ServerInfoModel>();
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

    public override void UpdateRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not ServerInfoModel newServer)
            throw new Exception("newServer is null in ServerInfoCruder.UpdateRecordWithKey");

        var crudersDictionary = GetCrudersDictionary();
        if (crudersDictionary is null)
            throw new Exception("crudersDictionary is null in ServerInfoCruder.UpdateRecordWithKey");

        crudersDictionary[recordName] = newServer;
    }

    protected override void AddRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not ServerInfoModel newServer)
            throw new Exception("newServer is null in ServerInfoCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);
        if (project == null)
            throw new Exception($"Project with name {_projectName} not found");
        project.ServerInfos.Add(recordName, newServer);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);
        if (project == null)
            throw new Exception($"Project with name {_projectName} not found");
        project.ServerInfos.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(string recordName, ItemData? defaultItemData)
    {
        return new ServerInfoModel();
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordName);


        //დასაშვები ინსტრუმენტების არჩევა
        itemSubMenuSet.AddMenuItem(
            new SelectServerAllowToolsCliMenuCommand(ParametersManager, _projectName, recordName),
            "Select Allow tools...");

        //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var server = parameters.GetServerByProject(_projectName, recordName);
        var project = parameters.GetProject(_projectName);

        if (server == null || project == null)
            return;
        foreach (var tool in ToolCommandFabric.ToolsByProjectsAndServers.Intersect(server.AllowToolsList ??
                     new List<ETools>()))
            itemSubMenuSet.AddMenuItem(
                new ToolTaskCliMenuCommand(_logger, tool, _projectName, recordName, ParametersManager),
                tool.ToString());
    }
}