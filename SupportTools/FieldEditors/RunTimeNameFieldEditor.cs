using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class RunTimeNameFieldEditor : FieldEditor<string>
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RunTimeNameFieldEditor(string propertyName, IParametersManager parametersManager) : base(propertyName)
    {
        _parametersManager = parametersManager;
    }

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        string? currentRunTimeName = GetValue(recordForUpdate);

        var runTimeCruder = RunTimeCruder.Create(_parametersManager);

        SetValue(recordForUpdate,
            await runTimeCruder.GetNameWithPossibleNewName(FieldName, currentRunTimeName, null, true,
                cancellationToken));
    }

    public override string GetValueStatus(object? record)
    {
        string? val = GetValue(record);

        if (val == null)
        {
            return string.Empty;
        }

        var runTimeCruder = RunTimeCruder.Create(_parametersManager);

        string? status = runTimeCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? string.Empty : $"({status})")}";
    }
}
