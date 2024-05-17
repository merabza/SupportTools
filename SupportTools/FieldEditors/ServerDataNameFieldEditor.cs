using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using System.Net.Http;

namespace SupportTools.FieldEditors;

public sealed class ServerDataNameFieldEditor : FieldEditor<string>
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerDataNameFieldEditor(ILogger logger, IHttpClientFactory httpClientFactory, string propertyName,
        IParametersManager parametersManager) : base(propertyName)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate) //, object currentRecord
    {
        ServerDataCruder serverDataCruder = new(_logger, _httpClientFactory, _parametersManager);
        SetValue(recordForUpdate, serverDataCruder.GetNameWithPossibleNewName(FieldName, GetValue(recordForUpdate)));
    }
}