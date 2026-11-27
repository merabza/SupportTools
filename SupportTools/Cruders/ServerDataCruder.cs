using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliParametersApiClientsEdit.FieldEditors;
using AppCliTools.CliTools.CliMenuCommands;
using LibSupportToolsServerWork.Registry;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportTools.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Cruders;

public sealed class ServerDataCruder : ParCruder<ServerDataModel>
{
    //სერვერის გამოყენების ადგილები სერვერის სახელით (ServerUsages). null — რაოდენობა სტატუსში არ ჩანს და ადგილების
    //სია მენიუში არ არის (სერვერის არჩევა)
    private readonly Func<string, IReadOnlyList<ServerUsage>>? _getUsages;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით DictionaryFieldEditor-ში
    // ReSharper disable once MemberCanBePrivate.Global
    public ServerDataCruder(ILogger logger, IHttpClientFactory httpClientFactory, IParametersManager parametersManager,
        Dictionary<string, ServerDataModel> currentValuesDictionary,
        Func<string, IReadOnlyList<ServerUsage>>? getUsages = null) : base(parametersManager,
        currentValuesDictionary, "Server", "Servers")
    {
        _getUsages = getUsages;
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
        FieldEditors.Add(new BoolFieldEditor(nameof(ServerDataModel.IsDecommissioned)));

        //ახალი ჩანაწერის შექმნისას ველები CheckFieldsEnables-ის პირველ გამოძახებამდე იკითხება
        EnableIsLocalEditor();
    }

    //თუ CurrentMachineServerName შევსებულია, IsLocal-ს ის განსაზღვრავს (ServersIsLocalCalculator). ხელით შეცვლილ
    //მნიშვნელობას პროგრამის შემდეგი გაშვება გადაწერდა, ამიტომ მაშინ ეს ველი არ ჩანს
    protected override void CheckFieldsEnables(ItemData itemData, string? lastEditedFieldName = null)
    {
        EnableIsLocalEditor();
    }

    //ახალი ან გადარქმეული სერვერის IsLocal-იც მაშინვე უნდა შეესაბამებოდეს CurrentMachineServerName-ს. ცვლილებას
    //ჩანაწერის შექმნის ან გადარქმევის ბოლოს შენახვა ინახავს
    protected override async ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        await base.AddRecordWithKey(recordKey, newRecord, cancellationToken);
        ServersIsLocalCalculator.Recalculate((SupportToolsParameters)ParametersManager.Parameters);
    }

    //გაუქმებული სერვერი სერვერების სიაშიც და სერვერის არჩევისასაც ჩანს, გამოყენების რაოდენობა კი მხოლოდ სერვერების
    //სიაში
    public override string? GetStatusFor(string name)
    {
        string? status =
            GetItemByName(name, false) is ServerDataModel { IsDecommissioned: true } ? "Decommissioned" : null;
        if (_getUsages is null)
        {
            return status;
        }

        string usageCount = $"Usage count: {_getUsages(name).Count}";
        return status is null ? usageCount : $"{status}, {usageCount}";
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        if (_getUsages is null)
        {
            return;
        }

        //სერვერის გამოყენების ადგილები ველების შემდეგ: არჩევისას პროგრამა ადგილის მენიუში გადადის
        foreach (ServerUsage usage in _getUsages(itemName))
        {
            itemSubMenuSet.AddMenuItem(new InfoCliMenuCommand(usage.Name, usage.MenuLink));
        }
    }

    private void EnableIsLocalEditor()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        EnableFieldByName(nameof(ServerDataModel.IsLocal),
            string.IsNullOrWhiteSpace(parameters.CurrentMachineServerName));
    }

    public static ServerDataCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        return new ServerDataCruder(logger, httpClientFactory, parametersManager, parameters.Servers);
    }
}
