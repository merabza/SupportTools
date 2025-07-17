using System.Collections.Generic;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class RedundantFileNameCruder : SimpleNamesListCruder
{
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RedundantFileNameCruder(IParametersManager parametersManager, string projectName) : base(
        "Redundant File Name", "Redundant File Names", false, false)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override List<string> GetList()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var project = parameters.GetProject(_projectName);
        return project?.RedundantFileNames ?? [];
    }
}