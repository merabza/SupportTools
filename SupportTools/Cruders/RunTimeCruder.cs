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

public sealed class RunTimeCruder : ParCruder
{
    public RunTimeCruder(IParametersManager parametersManager) : base(parametersManager, "RunTime", "RunTimes")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(TextItemData.Text)));
    }

    private Dictionary<string, string> GetRunTimes()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.RunTimes;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var runTimes = GetRunTimes();
        return runTimes.ToDictionary(k => k.Key, v => (ItemData)new TextItemData { Text = v.Value });
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var runTimes = GetRunTimes();
        return runTimes.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newRunTime)
            throw new Exception("newRunTime is null in RunTimeCruder.UpdateRecordWithKey");
        if (string.IsNullOrWhiteSpace(newRunTime.Text))
            throw new Exception("newRunTime.Text is empty in RunTimeCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.RunTimes[recordKey] = newRunTime.Text;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newRunTime)
            throw new Exception("newRunTime is null in RunTimeCruder.AddRecordWithKey");
        if (string.IsNullOrWhiteSpace(newRunTime.Text))
            throw new Exception("newRunTime.Text is empty in RunTimeCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.RunTimes.Add(recordKey, newRunTime.Text);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var runTimes = parameters.RunTimes;
        runTimes.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        GenerateStandardRunTimesCliMenuCommand generateCommand = new(ParametersManager);
        //"Generate standard RunTimes..."
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }
}