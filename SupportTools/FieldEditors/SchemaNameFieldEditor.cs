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

//ProdCopy ბაზის სქემის სახელის ამრჩევი
public sealed class SchemaNameFieldEditor : FieldEditor<string>
{
    private readonly string _connectionString;
    private readonly ILogger _logger;
    private readonly string _sideName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SchemaNameFieldEditor(string propertyName, ILogger logger, string sideName, string connectionString) :
        base(propertyName)
    {
        _logger = logger;
        _sideName = sideName;
        _connectionString = connectionString;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        //var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        //ProjectModel? project = parameters.GetProject(_connectionString);
        //if (project is null)
        //{
        //    StShared.WriteErrorLine($"Project {_connectionString} not found", true, _logger);
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

        List<string> schemas = [.. tables.Values.Select(t => t.SchemaName).Distinct().OrderBy(s => s)];
        if (schemas.Count == 0)
        {
            StShared.WriteErrorLine($"No schemas found in {_sideName} database", true, _logger);
            return ValueTask.CompletedTask;
        }

        string? current = GetValue(recordForUpdate);
        var selectInput = new SelectFromListInput(FieldName, schemas, current);
        if (!selectInput.DoInput() || string.IsNullOrEmpty(selectInput.Text))
        {
            return ValueTask.CompletedTask;
        }

        SetValue(recordForUpdate, selectInput.Text);
        return ValueTask.CompletedTask;
    }
}
