using System.Collections.Generic;
using System.Linq;
using CliParameters;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class RedundantFileNameCruder : ParCruder
{
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RedundantFileNameCruder(IParametersManager parametersManager, string projectName) : base(parametersManager,
        "Redundant File Name", "Redundant File Names", false, false)
    {
        _projectName = projectName;
    }

    private List<string> GetRedundantFileNames()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);
        return project?.RedundantFileNames ?? [];
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetRedundantFileNames().ToDictionary(k => k, v => (ItemData)new TextItemData { Text = v });
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var redundantFileNames = GetRedundantFileNames();
        return redundantFileNames.Contains(recordKey);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var redundantFileNames = GetRedundantFileNames();
        redundantFileNames.Remove(recordKey);
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projects = parameters.Projects;
        if (!projects.TryGetValue(_projectName, out var value))
            return;

        value.RedundantFileNames.Add(recordKey);
    }
}