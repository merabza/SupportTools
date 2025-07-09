using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersApiClientsEdit.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ServerDataCruder : ParCruder
{
    public ServerDataCruder(ILogger logger, IHttpClientFactory httpClientFactory, IParametersManager parametersManager)
        : base(parametersManager, "Server", "Servers")
    {
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerDataModel.IsLocal), true));
        FieldEditors.Add(new ApiClientNameFieldEditor(logger, httpClientFactory, nameof(ServerDataModel.WebAgentName),
            ParametersManager, true));
        FieldEditors.Add(new ApiClientNameFieldEditor(logger, httpClientFactory,
            nameof(ServerDataModel.WebAgentInstallerName), ParametersManager, true));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.FilesUserName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.FilesUsersGroupName)));
        FieldEditors.Add(new RunTimeNameFieldEditor(nameof(ServerDataModel.Runtime), ParametersManager));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.ServerSideDownloadFolder)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.ServerSideDeployFolder)));
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.Servers.ToDictionary(p => p.Key, p => (ItemData)p.Value);
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var servers = parameters.Servers;
        return servers.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not ServerDataModel newServer)
            throw new Exception("newServer is null in ServerDataCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Servers[recordKey] = newServer;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not ServerDataModel newServer)
            throw new Exception("newServer is null in ServerDataCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Servers.Add(recordKey, newServer);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var servers = parameters.Servers;
        servers.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(ItemData? defaultItemData)
    {
        return new ServerDataModel();
    }
}