using CliParameters.FieldEditors;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class EnvironmentNameFieldEditor : FieldEditor<string>
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EnvironmentNameFieldEditor(string propertyName, IParametersManager parametersManager) : base(propertyName,
        true)
    {
        _parametersManager = parametersManager;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate)
    {
        var currentEnvironmentName = GetValue(recordForUpdate);

        var environmentCruder = EnvironmentCruder.Create(_parametersManager);

        SetValue(recordForUpdate, environmentCruder.GetNameWithPossibleNewName(FieldName, currentEnvironmentName));
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val == null)
            return string.Empty;

        var environmentCruder = EnvironmentCruder.Create(_parametersManager);

        var status = environmentCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? string.Empty : $"({status})")}";
    }
}