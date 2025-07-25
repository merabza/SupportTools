﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsEdit.FieldEditors;
using CliParametersDataEdit.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportTools.FieldEditors;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ServerInfoCruder : ParCruder<ServerInfoModel>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly string _projectName;

    public ServerInfoCruder(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager,
        Dictionary<string, ServerInfoModel> currentValuesDictionary, string projectName) : base(parametersManager,
        currentValuesDictionary, "Server", "Servers", true)
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

    public static ServerInfoCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string projectName)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        var currentValuesDictionary = parameters.GetProject(projectName)?.ServerInfos ??
                                      throw new ArgumentNullException(nameof(projectName));
        return new ServerInfoCruder(logger, httpClientFactory, parametersManager, currentValuesDictionary, projectName);
    }

    //private Dictionary<string, ServerInfoModel> GetServerInfos()
    //{
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    var project = parameters.GetProject(_projectName);
    //    return project?.ServerInfos ?? [];
    //}

    //protected override Dictionary<string, ItemData> GetCrudersDictionary()
    //{
    //    return GetServerInfos().ToDictionary(p => p.Key, p => (ItemData)p.Value);
    //}

    //public override bool ContainsRecordWithKey(string recordKey)
    //{
    //    var itemDataDict = GetCrudersDictionary();

    //    return itemDataDict.ContainsKey(recordKey);
    //}

    //public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    if (newRecord is not ServerInfoModel newServer)
    //        throw new Exception("newServer is null in ServerInfoCruder.UpdateRecordWithKey");

    //    var crudersDictionary = GetCrudersDictionary() ??
    //                            throw new Exception(
    //                                "crudersDictionary is null in ServerInfoCruder.UpdateRecordWithKey");
    //    crudersDictionary[recordKey] = newServer;
    //}

    //protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    if (newRecord is not ServerInfoModel newServer)
    //        throw new Exception("newServer is null in ServerInfoCruder.AddRecordWithKey");
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    var project = parameters.GetProject(_projectName) ??
    //                  throw new Exception($"Project with name {_projectName} not found");
    //    project.ServerInfos.Add(recordKey, newServer);
    //}

    //protected override void RemoveRecordWithKey(string recordKey)
    //{
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    var project = parameters.GetProject(_projectName) ??
    //                  throw new Exception($"Project with name {_projectName} not found");
    //    project.ServerInfos.Remove(recordKey);
    //}

    //protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    //{
    //    return new ServerInfoModel();
    //}

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        //დასაშვები ინსტრუმენტების არჩევა
        itemSubMenuSet.AddMenuItem(
            new SelectServerAllowToolsCliMenuCommand(ParametersManager, _projectName, recordKey));

        //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var server = parameters.GetServerByProject(_projectName, recordKey);
        var project = parameters.GetProject(_projectName);

        if (server == null || project == null)
            return;
        foreach (var tool in Enum.GetValues<EProjectServerTools>().Intersect(server.AllowToolsList ?? []))
            itemSubMenuSet.AddMenuItem(new ProjectServerToolTaskCliMenuCommand(_logger, _httpClientFactory, tool,
                _projectName, server, ParametersManager));
    }
}