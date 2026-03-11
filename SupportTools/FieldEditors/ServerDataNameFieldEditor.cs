using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
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

    public override async ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default) //, object currentRecord
    {
        var serverDataCruder = ServerDataCruder.Create(_logger, _httpClientFactory, _parametersManager);
        SetValue(recordForUpdate,
            await serverDataCruder.GetNameWithPossibleNewName(FieldName, GetValue(recordForUpdate), null, false,
                cancellationToken));
    }
}
