using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Models;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class EnvironmentCruder : ParCruder
{
    public EnvironmentCruder(IParametersManager parametersManager) : base(parametersManager, "Environment",
        "Environments")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(EnvironmentData.Description)));
    }

    private Dictionary<string, string> GetEnvironments()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.Environments;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var environments = GetEnvironments();
        return environments.ToDictionary(k => k.Key, v => (ItemData)new EnvironmentData { Description = v.Value });
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var environments = GetEnvironments();
        return environments.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not EnvironmentData newEnvironment)
            throw new Exception("newEnvironment is null in EnvironmentCruder.UpdateRecordWithKey");
        if (string.IsNullOrWhiteSpace(newEnvironment.Description))
            throw new Exception("newEnvironment.Description is empty in EnvironmentCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Environments[recordName] = newEnvironment.Description;
    }

    protected override void AddRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not EnvironmentData newEnvironment)
            throw new Exception("newEnvironment is null in EnvironmentCruder.AddRecordWithKey");
        if (string.IsNullOrWhiteSpace(newEnvironment.Description))
            throw new Exception("newEnvironment.Description is empty in EnvironmentCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Environments.Add(recordName, newEnvironment.Description);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var environments = parameters.Environments;
        environments.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(string recordName, ItemData? defaultItemData)
    {
        return new EnvironmentData();
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        GenerateStandardEnvironmentsCommand generateCommand = new(ParametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand, "Generate standard Environments...");
    }
}