using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibMenuInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace SupportTools.FieldEditors;

//ბაზის ცხრილის ველის სახელის ამრჩევი — დაბმულია მშობელი ცხრილის _sideName მხარესთან
public sealed class FieldNameFieldEditor : FieldEditor<string>
{
    private readonly string _connectionString;
    private readonly ILogger _logger;
    private readonly string _schemaName;
    private readonly string _sideName;

    private readonly string _tableName;
    //private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FieldNameFieldEditor(string propertyName, ILogger logger, string sideName, string connectionString,
        string schemaName, string tableName) : base(propertyName)
    {
        _logger = logger;
        _sideName = sideName;
        _connectionString = connectionString;
        //_projectName = projectName;
        _schemaName = schemaName;
        _tableName = tableName;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        //var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        //ProjectModel? project = parameters.GetProject(_projectName);
        //if (project is null)
        //{
        //    StShared.WriteErrorLine($"Project {_projectName} not found", true, _logger);
        //    return ValueTask.CompletedTask;
        //}

        //PairedDbObjectsConnectionResolver? resolver =
        //    PairedDbObjectsConnectionResolver.Create(parameters, project, _logger);
        //if (resolver is null)
        //{
        //    return ValueTask.CompletedTask;
        //}

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? tables =
            DbSchemaQueryHelper.ReadTablesAndColumns(_connectionString, _sideName, _logger);
        if (tables is null)
        {
            return ValueTask.CompletedTask;
        }

        (string SchemaLower, string TableLower) key = (_schemaName.ToLowerInvariant(), _tableName.ToLowerInvariant());

        if (!tables.TryGetValue(key, out TableInfo? tableInfo))
        {
            StShared.WriteErrorLine($"{_sideName} table {_schemaName}.{_tableName} not found in database", true,
                _logger);
            return ValueTask.CompletedTask;
        }

        List<string> columns = [.. tableInfo.Columns.OrderBy(c => c)];
        if (columns.Count == 0)
        {
            StShared.WriteErrorLine($"No columns found in {_sideName} table {_schemaName}.{_tableName}", true, _logger);
            return ValueTask.CompletedTask;
        }

        string? current = GetValue(recordForUpdate);
        var selectInput = new SelectFromListInput(FieldName, columns, current);
        if (!selectInput.DoInput() || string.IsNullOrEmpty(selectInput.Text))
        {
            return ValueTask.CompletedTask;
        }

        SetValue(recordForUpdate, selectInput.Text);
        return ValueTask.CompletedTask;
    }
}
