using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class EnvironmentFieldEditor : FieldEditor<Dictionary<string, string>>
{
    private readonly ParametersManager _parametersManager;

    public EnvironmentFieldEditor(string propertyName, ParametersManager parametersManager) : base(propertyName, null,
        true)
    {
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var serverDataCruder = new EnvironmentCruder(_parametersManager);
        var menuSet = serverDataCruder.GetListMenu();
        return menuSet;
    }


    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val == null || val.Count <= 0)
            return "No Details";

        if (val.Count > 1)
            return $"{val.Count} Details";

        var kvp = val.Single();
        return kvp.Key;
    }
}