using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class RunTimeNameFieldEditor : FieldEditor<string>
{
    private readonly IParametersManager _parametersManager;

    public RunTimeNameFieldEditor(string propertyName,
        IParametersManager parametersManager) : base(propertyName)
    {
        _parametersManager = parametersManager;
    }

    public override void UpdateField(string? recordName, object recordForUpdate)
    {
        var currentRunTimeName = GetValue(recordForUpdate);

        var runTimeCruder = new RunTimeCruder(_parametersManager);

        SetValue(recordForUpdate,
            runTimeCruder.GetNameWithPossibleNewName(FieldName, currentRunTimeName));
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val == null)
            return "";

        var runTimeCruder = new RunTimeCruder(_parametersManager);

        var status = runTimeCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? "" : $"({status})")}";
    }
}