using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairProdCopyAndDevDbObjectsToolCommand : ToolCommand
{
    private const string ActionName = "Pair ProdCopy and Dev Db Objects";
    private const string ActionDescription = "Pair ProdCopy and Dev Db Objects by name and save to JSON";
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairProdCopyAndDevDbObjectsToolCommand(ILogger logger, PairDbObjectsParameters parameters,
        IParametersManager? parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
    }

    private PairDbObjectsParameters Parameters => (PairDbObjectsParameters)Par;

    protected override bool CheckValidate()
    {
        if (Parameters.ProdCopyDataProvider == EDatabaseProvider.None)
        {
            _logger.LogError("ProdCopyDataProvider not specified");
            return false;
        }

        if (Parameters.DevDataProvider == EDatabaseProvider.None)
        {
            _logger.LogError("DevDataProvider not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.ProdCopyConnectionString))
        {
            _logger.LogError("ProdCopyConnectionString not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.DevConnectionString))
        {
            _logger.LogError("DevConnectionString not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.ResultFileName))
        {
            _logger.LogError("ResultFileName not specified");
            return false;
        }

        return true;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        Dictionary<(string SchemaLower, string TableLower), TableInfo>? prodCopyTables =
            ReadTablesAndColumns(Parameters.ProdCopyConnectionString, "ProdCopy");
        if (prodCopyTables is null)
        {
            return false;
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? devTables =
            ReadTablesAndColumns(Parameters.DevConnectionString, "Dev");
        if (devTables is null)
        {
            return false;
        }

        var pairedTables = new List<PairedTable>();
        foreach (KeyValuePair<(string SchemaLower, string TableLower), TableInfo> prodCopyKvp in prodCopyTables)
        {
            if (!devTables.TryGetValue(prodCopyKvp.Key, out TableInfo? devTable))
            {
                continue;
            }

            TableInfo prodCopyTable = prodCopyKvp.Value;

            Dictionary<string, string> devColumnLookup =
                devTable.Columns.ToDictionary(c => c.ToLowerInvariant(), c => c);

            var pairedFields = new List<PairedField>();
            foreach (string prodColumn in prodCopyTable.Columns)
            {
                if (devColumnLookup.TryGetValue(prodColumn.ToLowerInvariant(), out string? devColumn))
                {
                    pairedFields.Add(new PairedField(prodColumn, devColumn));
                }
            }

            pairedTables.Add(new PairedTable(prodCopyTable.SchemaName, prodCopyTable.TableName, devTable.SchemaName,
                devTable.TableName, pairedFields));
        }

        var result = new PairedDbObjectsResult(pairedTables);

        try
        {
            string json = JsonConvert.SerializeObject(result, Formatting.Indented);
            await File.WriteAllTextAsync(Parameters.ResultFileName, json, cancellationToken);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Paired DB objects saved to {ResultFileName}. Paired tables: {Count}",
                    Parameters.ResultFileName, pairedTables.Count);
            }
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, true, _logger);
            return false;
        }

        return true;
    }

    private DbManager GetDbManager(string connectionString)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        return DbManager.Create(dbKit, connectionString) ?? throw new Exception("Cannot create DbManager");
    }

    private Dictionary<(string SchemaLower, string TableLower), TableInfo>? ReadTablesAndColumns(
        string connectionString, string sideName)
    {
        // ReSharper disable once using
        using DbManager dbm = GetDbManager(connectionString);
        try
        {
            const string query = """
                                 SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME
                                 FROM INFORMATION_SCHEMA.COLUMNS c
                                 INNER JOIN INFORMATION_SCHEMA.TABLES t
                                   ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
                                 WHERE t.TABLE_TYPE = 'BASE TABLE'
                                 ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
                                 """;
            dbm.Open();
            // ReSharper disable once using
            using IDataReader reader = dbm.ExecuteReader(query);
            var tables = new Dictionary<(string SchemaLower, string TableLower), TableInfo>();
            while (reader.Read())
            {
                string schema = (string)reader["TABLE_SCHEMA"];
                string table = (string)reader["TABLE_NAME"];
                string column = (string)reader["COLUMN_NAME"];

                (string SchemaLower, string TableLower) key = (schema.ToLowerInvariant(), table.ToLowerInvariant());
                if (!tables.TryGetValue(key, out TableInfo? tableInfo))
                {
                    tableInfo = new TableInfo(schema, table);
                    tables[key] = tableInfo;
                }

                tableInfo.Columns.Add(column);
            }

            return tables;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to read tables and columns from {sideName} database", true, _logger);
            return null;
        }
        finally
        {
            dbm.Close();
        }
    }
}
