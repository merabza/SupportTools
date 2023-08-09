using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class EnvironmentCruder : ParCruder
{
    public EnvironmentCruder(IParametersManager parametersManager) : base(parametersManager, "Environment",
        "Environments")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(TextItemData.Text)));
    }

    private Dictionary<string, string> GetEnvironments()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.Environments;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var environments = GetEnvironments();
        return environments.ToDictionary(k => k.Key, v => (ItemData)new TextItemData() { Text = v.Value });
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var environments = GetEnvironments();
        return environments.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newEnvironment)
            throw new Exception("newEnvironment is null in EnvironmentCruder.UpdateRecordWithKey");
        if (string.IsNullOrWhiteSpace(newEnvironment.Text))
            throw new Exception("newEnvironment.Description is empty in EnvironmentCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Environments[recordKey] = newEnvironment.Text;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newEnvironment)
            throw new Exception("newEnvironment is null in EnvironmentCruder.AddRecordWithKey");
        if (string.IsNullOrWhiteSpace(newEnvironment.Text))
            throw new Exception("newEnvironment.Description is empty in EnvironmentCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Environments.Add(recordKey, newEnvironment.Text);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var environments = GetEnvironments();
        environments.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        GenerateStandardEnvironmentsCommand generateCommand = new(ParametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand, "Generate standard Environments...");
    }
}