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

public sealed class RunTimeCruder : ParCruder
{
    public RunTimeCruder(IParametersManager parametersManager) : base(parametersManager, "RunTime", "RunTimes")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(RunTimeData.Description)));
    }

    private Dictionary<string, string> GetRunTimes()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.RunTimes;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var runTimes = GetRunTimes();
        return runTimes.ToDictionary(k => k.Key, v => (ItemData)new RunTimeData { Description = v.Value });
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var runTimes = GetRunTimes();
        return runTimes.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not RunTimeData newRunTime)
            throw new Exception("newRunTime is null in RunTimeCruder.UpdateRecordWithKey");
        if (string.IsNullOrWhiteSpace(newRunTime.Description))
            throw new Exception("newRunTime.Description is empty in RunTimeCruder.UpdateRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.RunTimes[recordName] = newRunTime.Description;
    }

    protected override void AddRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not RunTimeData newRunTime)
            throw new Exception("newRunTime is null in RunTimeCruder.AddRecordWithKey");
        if (string.IsNullOrWhiteSpace(newRunTime.Description))
            throw new Exception("newRunTime.Description is empty in RunTimeCruder.AddRecordWithKey");
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.RunTimes.Add(recordName, newRunTime.Description);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var runTimes = parameters.RunTimes;
        runTimes.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(string recordName, ItemData? defaultItemData)
    {
        return new RunTimeData();
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        GenerateStandardRunTimesCommand generateCommand = new(ParametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand, "Generate standard RunTimes...");
    }
}