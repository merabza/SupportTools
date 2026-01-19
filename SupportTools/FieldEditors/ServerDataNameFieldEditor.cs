using System.Net.Http;
using CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class ServerDataNameFieldEditor : FieldEditor<string>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerDataNameFieldEditor(ILogger logger, IHttpClientFactory httpClientFactory, string propertyName,
        IParametersManager parametersManager) : base(propertyName, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate) //, object currentRecord
    {
        var serverDataCruder = ServerDataCruder.Create(_logger, _httpClientFactory, _parametersManager);
        SetValue(recordForUpdate, serverDataCruder.GetNameWithPossibleNewName(FieldName, GetValue(recordForUpdate)));
    }
}