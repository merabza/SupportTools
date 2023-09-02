using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

public sealed class ProjectsFieldEditor : FieldEditor<Dictionary<string, ProjectModel>>
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    public ProjectsFieldEditor(ILogger logger, string propertyName, ParametersManager parametersManager) : base(
        propertyName, false, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var projectCruder = new ProjectCruder(_logger, _parametersManager);
        var menuSet = projectCruder.GetListMenu();
        return menuSet;
    }


    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val is not { Count: > 0 })
            return "No Details";

        if (val.Count > 1)
            return $"{val.Count} Details";

        var kvp = val.Single();
        return kvp.Key;
    }
}