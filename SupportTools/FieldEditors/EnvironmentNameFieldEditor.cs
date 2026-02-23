using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
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

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        string? currentEnvironmentName = GetValue(recordForUpdate);

        var environmentCruder = EnvironmentCruder.Create(_parametersManager);

        SetValue(recordForUpdate,
            await environmentCruder.GetNameWithPossibleNewName(FieldName, currentEnvironmentName, null, false,
                cancellationToken));
    }

    public override string GetValueStatus(object? record)
    {
        string? val = GetValue(record);

        if (val == null)
        {
            return string.Empty;
        }

        var environmentCruder = EnvironmentCruder.Create(_parametersManager);

        string? status = environmentCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? string.Empty : $"({status})")}";
    }
}
