using CliMenu;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using System.Net.Http;

namespace SupportTools.FieldEditors;

public sealed class DatabasesExchangeParametersFieldEditor : FieldEditor<DatabasesExchangeParameters>
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabasesExchangeParametersFieldEditor(ILogger logger, IHttpClientFactory httpClientFactory,
        string databasesExchangeParametersName, ParametersManager parametersManager) : base(
        databasesExchangeParametersName, false, null, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);
        if (val is null)
            return "(empty)";
        //EDataProvider dataProvider = val.DataProvider;
        var status = "Some Parameters";
        //DbConnectionParameters dbConnectionParameters =
        //    DbConnectionFabric.GetDbConnectionParameters(dataProvider, val.ConnectionString);
        //status +=
        //    $", Connection: {(dbConnectionParameters == null ? "(invalid)" : dbConnectionParameters.GetStatus())}";
        //status += $", CommandTimeOut: {val.CommandTimeOut}";
        return status;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var databasesExchangeParameters =
            GetValue(record) ?? new DatabasesExchangeParameters();

        var serverDatabasesExchangeParametersManager =
            new ServerDatabasesExchangeParametersManager(databasesExchangeParameters, _parametersManager, this,
                record);

        var parametersEditor = new ServerDatabasesExchangeParametersEditor(_logger, _httpClientFactory,
            serverDatabasesExchangeParametersManager, _parametersManager);

        return parametersEditor.GetParametersMainMenu();
    }
}