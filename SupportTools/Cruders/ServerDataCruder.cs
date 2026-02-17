using System.Collections.Generic;
using System.Net.Http;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliParametersApiClientsEdit.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ServerDataCruder : ParCruder<ServerDataModel>
{
    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით DictionaryFieldEditor-ში
    // ReSharper disable once MemberCanBePrivate.Global
    public ServerDataCruder(ILogger logger, IHttpClientFactory httpClientFactory, IParametersManager parametersManager,
        Dictionary<string, ServerDataModel> currentValuesDictionary) : base(parametersManager, currentValuesDictionary,
        "Server", "Servers")
    {
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerDataModel.IsLocal), true));
        FieldEditors.Add(new ApiClientNameFieldEditor(nameof(ServerDataModel.WebAgentName), logger, httpClientFactory,
            ParametersManager, true));
        FieldEditors.Add(new ApiClientNameFieldEditor(nameof(ServerDataModel.WebAgentInstallerName), logger,
            httpClientFactory, ParametersManager, true));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.FilesUserName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.FilesUsersGroupName)));
        FieldEditors.Add(new RunTimeNameFieldEditor(nameof(ServerDataModel.Runtime), ParametersManager));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.ServerSideDownloadFolder)));
        FieldEditors.Add(new TextFieldEditor(nameof(ServerDataModel.ServerSideDeployFolder)));
    }

    public static ServerDataCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        return new ServerDataCruder(logger, httpClientFactory, parametersManager, parameters.Servers);
    }
}
