using System;
using System.Collections.Generic;
using System.Data;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

//ცხრილებისა და სვეტების ერთიანი წამკითხავი ორივე ბაზის INFORMATION_SCHEMA-დან
public static class DbSchemaQueryHelper
{
    private const string Query = """
                                 SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME
                                 FROM INFORMATION_SCHEMA.COLUMNS c
                                 INNER JOIN INFORMATION_SCHEMA.TABLES t
                                   ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
                                 WHERE t.TABLE_TYPE = 'BASE TABLE'
                                 ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
                                 """;

    public static Dictionary<(string SchemaLower, string TableLower), TableInfo>? ReadTablesAndColumns(
        string connectionString, string sideName, ILogger logger)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using DbManager? dbm = DbManager.Create(dbKit, connectionString);
        if (dbm is null)
        {
            StShared.WriteErrorLine($"Cannot create DbManager for {sideName} database", true, logger);
            return null;
        }

        try
        {
            dbm.Open();
            // ReSharper disable once using
            using IDataReader reader = dbm.ExecuteReader(Query);
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
            StShared.WriteException(ex, $"Failed to read tables and columns from {sideName} database", true, logger);
            return null;
        }
        finally
        {
            dbm.Close();
        }
    }

    //case-sensitive ვერსია: კლავიში ინახავს რეგისტრს ისე, როგორც INFORMATION_SCHEMA-ში არის
    public static Dictionary<(string Schema, string Table), TableInfo>? ReadTablesAndColumnsCaseSensitive(
        string connectionString, string sideName, ILogger logger)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using DbManager? dbm = DbManager.Create(dbKit, connectionString);
        if (dbm is null)
        {
            StShared.WriteErrorLine($"Cannot create DbManager for {sideName} database", true, logger);
            return null;
        }

        try
        {
            dbm.Open();
            // ReSharper disable once using
            using IDataReader reader = dbm.ExecuteReader(Query);
            var tables = new Dictionary<(string Schema, string Table), TableInfo>();
            while (reader.Read())
            {
                string schema = (string)reader["TABLE_SCHEMA"];
                string table = (string)reader["TABLE_NAME"];
                string column = (string)reader["COLUMN_NAME"];

                (string Schema, string Table) key = (schema, table);
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
            StShared.WriteException(ex, $"Failed to read tables and columns from {sideName} database", true, logger);
            return null;
        }
        finally
        {
            dbm.Close();
        }
    }
}
