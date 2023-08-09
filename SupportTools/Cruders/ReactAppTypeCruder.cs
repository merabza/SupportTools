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
        "React App Type", "React App Types")
    {
        _logger = logger;
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(TextItemData.Text), "TemplateName"));
    }

    private Dictionary<string, string> GetReactAppTemplateNames()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.ReactAppTemplates;
    }


    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetReactAppTemplateNames()
            .ToDictionary(k => k.Key, v => (ItemData)new TextItemData { Text = v.Value });
    }

    protected override ItemData CreateNewItem(ItemData? defaultItemData)
    {
        return new TextItemData();
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

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newReactAppType)
            throw new Exception("newReactAppType is null in ReactAppTypeCruder.UpdateRecordWithKey");
        if (string.IsNullOrWhiteSpace(newReactAppType.Text))
            throw new Exception("newReactAppType.Description is empty in EnvironmentCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.ReactAppTemplates[recordKey] = newReactAppType.Text;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newReactAppType)
            throw new Exception("newReactAppType is null in ReactAppTypeCruder.AddRecordWithKey");
        if (string.IsNullOrWhiteSpace(newReactAppType.Text))
            throw new Exception("newEnvironment.Description is empty in EnvironmentCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.ReactAppTemplates.Add(recordKey, newReactAppType.Text);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        ReCreateAllReactAppsByTemplatesCliMenuCommand reCreateAllReactAppsByTemplatesCliMenuCommand =
            new(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(reCreateAllReactAppsByTemplatesCliMenuCommand,
            "Recreate All React App files By Templates...");
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        var reactAppTemplateNames = GetReactAppTemplateNames();
        if (!reactAppTemplateNames.ContainsKey(recordKey))
            return;

        //UpdateGitProjectCliMenuCommand updateGitProjectCommand = new(_logger, recordKey, ParametersManager);
        //itemSubMenuSet.AddMenuItem(updateGitProjectCommand);

        ReCreateReactAppByTemplateNameCommand reCreateReactCommand =
            new(_logger, ParametersManager, recordKey, reactAppTemplateNames[recordKey]);
        itemSubMenuSet.AddMenuItem(reCreateReactCommand, "Recreate React App files By Template...");
    }
}