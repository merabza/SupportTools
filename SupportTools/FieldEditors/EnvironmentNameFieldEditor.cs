using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class EnvironmentNameFieldEditor : FieldEditor<string>
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EnvironmentNameFieldEditor(string propertyName,
        IParametersManager parametersManager) : base(propertyName)
    {
        _parametersManager = parametersManager;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate)
    {
        var currentEnvironmentName = GetValue(recordForUpdate);

        var environmentCruder = new EnvironmentCruder(_parametersManager);

        SetValue(recordForUpdate,
            environmentCruder.GetNameWithPossibleNewName(FieldName, currentEnvironmentName));
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val == null)
            return "";

        var environmentCruder = new EnvironmentCruder(_parametersManager);

        var status = environmentCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? "" : $"({status})")}";
    }
}