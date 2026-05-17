using System;
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

//ბაზის ცხრილის სახელის ამრჩევი — გაფილტრულია მიმდინარე სქემით
public sealed class TableNameFieldEditor : FieldEditor<string>
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    private readonly string _sideName;
    //private readonly IParametersManager _parametersManager;
    //private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TableNameFieldEditor(string propertyName, ILogger logger, string sideName, string connectionString) :
        base(propertyName)
    {
        _logger = logger;
        _sideName = sideName;
        _connectionString = connectionString;
        //_parametersManager = parametersManager;
        //_projectName = projectName;
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

        string? schemaName = GetValue<string>(recordForUpdate, $"{_sideName}SchemaName");
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            StShared.WriteErrorLine($"Set {_sideName}SchemaName before choosing a table", true, _logger);
            return ValueTask.CompletedTask;
        }

        List<string> tableNames =
        [
            .. tables.Values.Where(t => string.Equals(t.SchemaName, schemaName, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.TableName).OrderBy(n => n)
        ];

        if (tableNames.Count == 0)
        {
            StShared.WriteErrorLine($"No tables found in {_sideName} schema {schemaName}", true, _logger);
            return ValueTask.CompletedTask;
        }

        string? current = GetValue(recordForUpdate);
        var selectInput = new SelectFromListInput(FieldName, tableNames, current);
        if (!selectInput.DoInput() || string.IsNullOrEmpty(selectInput.Text))
        {
            return ValueTask.CompletedTask;
        }

        SetValue(recordForUpdate, selectInput.Text);
        return ValueTask.CompletedTask;
    }
}
