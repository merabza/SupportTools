using System.Collections.Generic;
using System.Linq;
using CliParameters;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class RedundantFileNameCruder : ParCruder
{
    private readonly string _projectName;

    public RedundantFileNameCruder(IParametersManager parametersManager, string projectName) : base(parametersManager,
        "Redundant File Name", "Redundant File Names", false, false)
    {
        _projectName = projectName;
    }

    private List<string> GetRedundantFileNames()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var project = parameters.GetProject(_projectName);
        return project?.RedundantFileNames ?? new List<string>();
    }


    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetRedundantFileNames().ToDictionary(k => k, v => (ItemData)new TextItemData { Text = v });
    }

    protected override ItemData CreateNewItem(string recordKey, ItemData? defaultItemData)
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
        if (!projects.ContainsKey(_projectName))
            return;
        var project = projects[_projectName];

        project.RedundantFileNames.Add(recordKey);
    }
}