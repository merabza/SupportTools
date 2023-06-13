using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ReactAppTypeCruder : ParCruder
{
    private readonly ILogger _logger;

    public ReactAppTypeCruder(ILogger logger, IParametersManager parametersManager) : base(parametersManager,
        "React App Type", "React App Types", true, false)
    {
        _logger = logger;
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(OptionalTextItemData.Text), "TemplateName"));
    }

    private Dictionary<string, string?> GetReactAppTemplateNames()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.ReactAppTemplates;
    }


    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetReactAppTemplateNames()
            .ToDictionary(k => k.Key, v => (ItemData)new OptionalTextItemData { Text = v.Value });
    }

    protected override ItemData CreateNewItem(string recordName, ItemData? defaultItemData)
    {
        return new OptionalTextItemData();
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var reactAppTemplateNames = GetReactAppTemplateNames();
        return reactAppTemplateNames.ContainsKey(recordKey);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var reactAppTemplateNames = GetReactAppTemplateNames();
        reactAppTemplateNames.Remove(recordKey);
    }

    public override void UpdateRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not OptionalTextItemData newReactAppType)
            throw new Exception("newReactAppType is null in ReactAppTypeCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.ReactAppTemplates[recordName] = newReactAppType.Text;
    }

    protected override void AddRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not OptionalTextItemData newReactAppType)
            throw new Exception("newReactAppType is null in ReactAppTypeCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.ReactAppTemplates.Add(recordName, newReactAppType.Text);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        ReCreateAllReactAppsByTemplatesCliMenuCommand reCreateAllReactAppsByTemplatesCliMenuCommand =
            new(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(reCreateAllReactAppsByTemplatesCliMenuCommand,
            "Recreate All React App files By Templates...");
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordName);

        var reactAppTemplateNames = GetReactAppTemplateNames();
        if (!reactAppTemplateNames.ContainsKey(recordName))
            return;

        //UpdateGitProjectCliMenuCommand updateGitProjectCommand = new(_logger, recordName, ParametersManager);
        //itemSubMenuSet.AddMenuItem(updateGitProjectCommand);

        ReCreateReactAppByTemplateNameCommand reCreateReactCommand =
            new(_logger, ParametersManager, recordName, reactAppTemplateNames[recordName]);
        itemSubMenuSet.AddMenuItem(reCreateReactCommand, "Recreate React App files By Template...");
    }
}