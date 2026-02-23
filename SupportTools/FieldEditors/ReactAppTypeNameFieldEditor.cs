using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class ReactAppTypeNameFieldEditor : FieldEditor<string>
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReactAppTypeNameFieldEditor(ILogger logger, string propertyName, IParametersManager parametersManager) :
        base(propertyName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        string? currentReactAppTypeName = GetValue(recordForUpdate);

        var reactAppTypeCruder = ReactAppTypeCruder.Create(_logger, _parametersManager);

        string? newValue =
            await reactAppTypeCruder.GetNameWithPossibleNewName(FieldName, currentReactAppTypeName, null, true,
                cancellationToken);

        SetValue(recordForUpdate, newValue);
    }

    public override string GetValueStatus(object? record)
    {
        string? val = GetValue(record);

        if (val == null)
        {
            return string.Empty;
        }

        var reactAppTypeCruder = ReactAppTypeCruder.Create(_logger, _parametersManager);

        string? status = reactAppTypeCruder.GetStatusFor(val);
        return $"{val} {(string.IsNullOrWhiteSpace(status) ? string.Empty : $"({status})")}";
    }
}
