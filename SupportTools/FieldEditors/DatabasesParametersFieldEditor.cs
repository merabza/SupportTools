using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using LibDatabaseParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.ParametersEditors;

namespace SupportTools.FieldEditors;

public sealed class DatabasesParametersFieldEditor : ParametersFieldEditor<DatabasesParameters,
    DatabaseParametersEditor>
{
    private readonly IHttpClientFactory _httpClientFactory;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabasesParametersFieldEditor(ILogger logger, IHttpClientFactory httpClientFactory, string propertyName,
        IParametersManager parametersManager) : base(logger, propertyName, parametersManager)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override DatabaseParametersEditor CreateEditor(object record, DatabasesParameters currentValue)
    {
        var serverDatabasesExchangeParametersManager =
            new SubParametersManager<DatabasesParameters>(currentValue, ParametersManager, this, record);

        return new DatabaseParametersEditor(Logger, _httpClientFactory, serverDatabasesExchangeParametersManager,
            ParametersManager);
    }
}