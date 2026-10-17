using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using DatabaseTools.OleDbTools;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

//ცხრილებისა და სვეტების ერთიანი წამკითხავი ორივე ბაზისთვის — SQL Server-ისა და MariaDB-ის INFORMATION_SCHEMA-დან
//ან Access-ის (OleDb) სქემის rowset-ებიდან
public static class DbSchemaQueryHelper
{
    private const string Query = """
                                 SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME
                                 FROM INFORMATION_SCHEMA.COLUMNS c
                                 INNER JOIN INFORMATION_SCHEMA.TABLES t
                                   ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
                                 WHERE t.TABLE_TYPE = 'BASE TABLE'
                                   AND t.TABLE_NAME <> '__EFMigrationsHistory' -- EF-ის მიგრაციების ისტორია არ წყვილდება: Dev-ს საკუთარი აქვს
                                 ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
                                 """;

    //MariaDB-ში სქემა ბაზაა — იკითხება მხოლოდ კავშირის მიმდინარე ბაზა, სქემის სახელად კი ცარიელი სტრიქონი ბრუნდება,
    //როგორც Access-ის შემთხვევაში
    private const string MariaDbQuery = """
                                        SELECT '' AS TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME
                                        FROM INFORMATION_SCHEMA.COLUMNS c
                                        INNER JOIN INFORMATION_SCHEMA.TABLES t
                                          ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
                                        WHERE t.TABLE_SCHEMA = DATABASE() AND t.TABLE_TYPE = 'BASE TABLE'
                                          AND t.TABLE_NAME <> '__EFMigrationsHistory'
                                        ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION
                                        """;

    //Access-ს (OleDb) და MariaDB-ს სქემები არ აქვს — სქემის სახელი ცარიელია და ცხრილები მხოლოდ სახელით წყვილდება
    public static bool UsesSchemas(EDatabaseProvider dataProvider)
    {
        return dataProvider is not (EDatabaseProvider.OleDb or EDatabaseProvider.MariaDb);
    }

    public static Dictionary<(string SchemaLower, string TableLower), TableInfo>? ReadTablesAndColumns(
        EDatabaseProvider dataProvider, string connectionString, string sideName, ILogger logger)
    {
        if (dataProvider == EDatabaseProvider.OleDb)
        {
            return ReadOleDbTablesAndColumns(connectionString, sideName, true, logger);
        }

        try
        {
            // ReSharper disable once using
            using var dbm = DbManager.Create(DbKitFactory.GetKit(dataProvider), connectionString);
            if (dbm is null)
            {
                StShared.WriteErrorLine($"Cannot create DbManager for {sideName} database", true, logger);
                return null;
            }

            try
            {
                dbm.Open();
                // ReSharper disable once using
                using IDataReader reader = dbm.ExecuteReader(GetColumnsQuery(dataProvider));
                var tables = new Dictionary<(string SchemaLower, string TableLower), TableInfo>();
                while (reader.Read())
                {
                    string schema = (string)reader["TABLE_SCHEMA"];
                    string table = (string)reader["TABLE_NAME"];
                    string column = (string)reader["COLUMN_NAME"];

                    (string SchemaLower, string TableLower) key = (schema.ToUpperInvariant(), table.ToUpperInvariant());
                    if (!tables.TryGetValue(key, out TableInfo? tableInfo))
                    {
                        tableInfo = new TableInfo(schema, table);
                        tables[key] = tableInfo;
                    }

                    tableInfo.Columns.Add(column);
                }

                return tables;
            }
            finally
            {
                dbm.Close();
            }
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to read tables and columns from {sideName} database", true, logger);
            return null;
        }
    }

    //case-sensitive ვერსია: კლავიში ინახავს რეგისტრს ისე, როგორც INFORMATION_SCHEMA-ში არის
    public static Dictionary<(string Schema, string Table), TableInfo>? ReadTablesAndColumnsCaseSensitive(
        EDatabaseProvider dataProvider, string connectionString, string sideName, ILogger logger)
    {
        if (dataProvider == EDatabaseProvider.OleDb)
        {
            return ReadOleDbTablesAndColumns(connectionString, sideName, false, logger);
        }

        try
        {
            // ReSharper disable once using
            using var dbm = DbManager.Create(DbKitFactory.GetKit(dataProvider), connectionString);
            if (dbm is null)
            {
                StShared.WriteErrorLine($"Cannot create DbManager for {sideName} database", true, logger);
                return null;
            }

            try
            {
                dbm.Open();
                // ReSharper disable once using
                using IDataReader reader = dbm.ExecuteReader(GetColumnsQuery(dataProvider));
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
            finally
            {
                dbm.Close();
            }
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to read tables and columns from {sideName} database", true, logger);
            return null;
        }
    }

    //Access-ისა და MariaDB-ის ProdCopy-ს სქემები არ აქვს — Dev ცხრილების ლექსიკონი მხოლოდ ცხრილის სახელით გადაიკეთება,
    //რომ ProdCopy-ის ("", table) გასაღებები დაემთხვეს; სხვადასხვა სქემაში ერთნაირსახელიანი ცხრილები გამოტოვდება გაფრთხილებით
    public static Dictionary<(string SchemaLower, string TableLower), TableInfo> ReKeyDevTablesByTableNameOnly(
        Dictionary<(string SchemaLower, string TableLower), TableInfo> devTables, ILogger logger)
    {
        var result = new Dictionary<(string SchemaLower, string TableLower), TableInfo>();
        var duplicates = new HashSet<string>();
        foreach (var kvp in devTables.Where(kvp => !result.TryAdd((string.Empty, kvp.Key.TableLower), kvp.Value)))
        {
            duplicates.Add(kvp.Key.TableLower);
        }

        foreach (string duplicate in duplicates)
        {
            result.Remove((string.Empty, duplicate));
            StShared.WriteWarningLine(
                $"Dev table name '{duplicate}' exists in multiple schemas — cannot pair with ProdCopy table by name only, skipped",
                true, logger);
        }

        return result;
    }

    private static string GetColumnsQuery(EDatabaseProvider dataProvider)
    {
        return dataProvider == EDatabaseProvider.MariaDb ? MariaDbQuery : Query;
    }

    //Access-ის (OleDb) ბაზას INFORMATION_SCHEMA და სქემები არ აქვს — ცხრილები OleDbSchemaReader-ით იკითხება,
    //სქემის სახელად ცარიელი სტრიქონი ინახება; __EFMigrationsHistory Access-ში ბუნებრივად არ არსებობს
    private static Dictionary<(string, string), TableInfo>? ReadOleDbTablesAndColumns(string connectionString,
        string sideName, bool lowerCaseKeys, ILogger logger)
    {
        try
        {
            var tables = new Dictionary<(string, string), TableInfo>();
            foreach ((string tableName, List<string> columns) in OleDbSchemaReader.ReadTablesAndColumns(
                         connectionString))
            {
                var tableInfo = new TableInfo(string.Empty, tableName);
                tableInfo.Columns.AddRange(columns);
                tables[(string.Empty, lowerCaseKeys ? tableName.ToUpperInvariant() : tableName)] = tableInfo;
            }

            return tables;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to read tables and columns from {sideName} database", true, logger);
            return null;
        }
    }
}
