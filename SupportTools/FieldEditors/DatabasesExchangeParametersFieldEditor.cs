using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ParametersEditors;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

public sealed class DatabasesExchangeParametersFieldEditor : ParametersFieldEditor<DatabasesExchangeParameters,
    ServerDatabasesExchangeParametersEditor>
{
    private readonly IHttpClientFactory _httpClientFactory;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabasesExchangeParametersFieldEditor(ILogger logger, IHttpClientFactory httpClientFactory,
        string propertyName, IParametersManager parametersManager) : base(logger, propertyName, parametersManager)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override ServerDatabasesExchangeParametersEditor CreateEditor(object record,
        DatabasesExchangeParameters currentValue)
    {
        var serverDatabasesExchangeParametersManager =
            new SubParametersManager<DatabasesExchangeParameters>(currentValue, ParametersManager, this, record);

        return new ServerDatabasesExchangeParametersEditor(Logger, _httpClientFactory,
            serverDatabasesExchangeParametersManager, ParametersManager);
    }
}