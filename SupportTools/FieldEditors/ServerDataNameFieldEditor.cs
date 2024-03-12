using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class ServerDataNameFieldEditor : FieldEditor<string>
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerDataNameFieldEditor(ILogger logger, string propertyName, IParametersManager parametersManager) : base(
        propertyName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate) //, object currentRecord
    {
        ServerDataCruder serverDataCruder = new(_logger, _parametersManager);
        SetValue(recordForUpdate, serverDataCruder.GetNameWithPossibleNewName(FieldName, GetValue(recordForUpdate)));
    }
}