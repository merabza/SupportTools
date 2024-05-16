using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class RunTimesFieldEditor : FieldEditor<Dictionary<string, string>>
{
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RunTimesFieldEditor(string propertyName, ParametersManager parametersManager) : base(propertyName, false,
        null, true)
    {
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var serverDataCruder = new RunTimeCruder(_parametersManager);
        var menuSet = serverDataCruder.GetListMenu();
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