using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//SqlBulkCopy მონაცემებს კავშირების (FK) შემოწმების გარეშე წერს და SQL Server ჩატვირთული ცხრილების კავშირებს
//untrusted-ად ნიშნავს (SSMS-ში "Check Existing Data On Creation Or Re-Enabling" = No).
//ამიტომ გადატანის ბოლოს თითოეული ცხრილის ყველა კავშირი არსებული მონაცემების შემოწმებით თავიდან ირთვება —
//WITH CHECK CHECK CONSTRAINT ALL გადაამოწმებს ჩატვირთულ მონაცემებს და კავშირებს ისევ სანდოს (trusted) გახდის
internal static class DevFkRevalidator
{
    public static bool Revalidate(string devConnectionString, IReadOnlyList<(string Schema, string Table)> tables,
        int commandTimeOut, ILogger logger)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using var dbm = DbManager.Create(dbKit, devConnectionString, commandTimeOut);
        if (dbm is null)
        {
            StShared.WriteErrorLine("Cannot create DbManager for Dev database", true, logger);
            return false;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Re-validating foreign keys on {TableCount} tables", tables.Count);
        }

        bool success = true;
        try
        {
            dbm.Open();
            foreach ((string Schema, string Table) node in tables)
            {
                try
                {
                    dbm.ExecuteNonQuery($"ALTER TABLE [{node.Schema}].[{node.Table}] WITH CHECK CHECK CONSTRAINT ALL");
                }
                catch (Exception ex)
                {
                    success = false;
                    //სტექტრეისის ნაცვლად მომხმარებლისთვის გასაგები დიაგნოსტიკა: რომელ კავშირს რამდენი და
                    //რომელი მნიშვნელობები აკლია; თუ ობოლი მწკრივი ვერ მოიძებნა, მიზეზი სხვაა — გამონაკლისი გამოვიტანოთ
                    if (!TryReportOrphanRows(dbm, node, logger))
                    {
                        StShared.WriteException(ex, $"Foreign key re-validation failed for {node.Schema}.{node.Table}",
                            true, logger);
                    }
                }
            }

            if (!success)
            {
                StShared.WriteErrorLine(
                    "Foreign keys on the tables listed above could not be re-enabled and remain unchecked. " +
                    "Fix or delete the listed rows (usually in the ProdCopy source) and run the transfer again.", true,
                    logger);
            }

            return success;
        }
        finally
        {
            dbm.Close();
        }
    }

    //ჩავარდნილი ცხრილის თითოეულ კავშირზე ითვლის ობოლ მწკრივებს (შვილის მნიშვნელობა, რომელიც მშობელ ცხრილში
    //არ არსებობს) და კონკრეტულ შეტყობინებას წერს; აბრუნდება მოიძებნა თუ არა ერთი ობოლი მაინც
    private static bool TryReportOrphanRows(DbManager dbm, (string Schema, string Table) node, ILogger logger)
    {
        try
        {
            List<FkColumn> fkColumns = ReadFkColumns(dbm, node);

            bool reportedAny = false;
            foreach (IGrouping<string, FkColumn> fk in fkColumns.GroupBy(f => f.FkName))
            {
                List<FkColumn> cols = [.. fk.OrderBy(o => o.Ordinal)];
                string notNullCond = string.Join(" AND ", cols.Select(c => $"c.[{c.Column}] IS NOT NULL"));
                string joinCond = string.Join(" AND ", cols.Select(c => $"r.[{c.RefColumn}] = c.[{c.Column}]"));
                string refTableFull = $"[{cols[0].RefSchema}].[{cols[0].RefTable}]";

                long orphanCount = dbm.ExecuteScalar<long>(
                    $"SELECT COUNT_BIG(*) FROM [{node.Schema}].[{node.Table}] AS c WHERE {notNullCond} AND NOT EXISTS (SELECT 1 FROM {refTableFull} AS r WHERE {joinCond})");
                if (orphanCount <= 0)
                {
                    continue;
                }

                reportedAny = true;
                string columnNames = string.Join(", ", cols.Select(c => c.Column));
                string refColumnNames = string.Join(", ", cols.Select(c => c.RefColumn));
                string samples = cols.Count == 1 ? GetSampleValues(dbm, node, cols[0]) : string.Empty;
                StShared.WriteErrorLine(
                    $"{node.Schema}.{node.Table}: {orphanCount} rows have {columnNames} values that do not exist in {cols[0].RefSchema}.{cols[0].RefTable} ({refColumnNames}) — constraint {fk.Key} cannot be enabled.{samples}",
                    true, logger, false);
            }

            return reportedAny;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to analyze orphan rows for {node.Schema}.{node.Table}", true, logger,
                false);
            return false;
        }
    }

    //ცხრილის ყველა კავშირის სვეტების აღწერა sys კატალოგიდან
    private static List<FkColumn> ReadFkColumns(DbManager dbm, (string Schema, string Table) node)
    {
        string query = $"""
                        SELECT fk.name AS FkName, fkc.constraint_column_id AS Ordinal, pc.name AS ColumnName,
                            rs.name AS RefSchemaName, rt.name AS RefTableName, rc.name AS RefColumnName
                        FROM sys.foreign_keys fk
                        INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                        INNER JOIN sys.columns pc ON pc.object_id = fk.parent_object_id AND pc.column_id = fkc.parent_column_id
                        INNER JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
                        INNER JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
                        INNER JOIN sys.columns rc ON rc.object_id = fk.referenced_object_id AND rc.column_id = fkc.referenced_column_id
                        WHERE fk.parent_object_id = OBJECT_ID(N'[{node.Schema}].[{node.Table}]')
                        """;
        // ReSharper disable once using
        using IDataReader reader = dbm.ExecuteReader(query);
        var result = new List<FkColumn>();
        while (reader.Read())
        {
            result.Add(new FkColumn((string)reader["FkName"], (int)reader["Ordinal"], (string)reader["ColumnName"],
                (string)reader["RefSchemaName"], (string)reader["RefTableName"], (string)reader["RefColumnName"]));
        }

        return result;
    }

    //ერთსვეტიანი კავშირისთვის ნაკლული მნიშვნელობების პირველი 10 ნიმუში
    private static string GetSampleValues(DbManager dbm, (string Schema, string Table) node, FkColumn col)
    {
        string query =
            $"SELECT DISTINCT TOP (10) c.[{col.Column}] FROM [{node.Schema}].[{node.Table}] AS c WHERE c.[{col.Column}] IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [{col.RefSchema}].[{col.RefTable}] AS r WHERE r.[{col.RefColumn}] = c.[{col.Column}]) ORDER BY c.[{col.Column}]";
        // ReSharper disable once using
        using IDataReader reader = dbm.ExecuteReader(query);
        var samples = new List<string>();
        while (reader.Read())
        {
            samples.Add(Convert.ToString(reader[0], CultureInfo.InvariantCulture) ?? "NULL");
        }

        return samples.Count == 0
            ? string.Empty
            : $" Missing values (first {samples.Count}): {string.Join(", ", samples)}.";
    }

    private sealed record FkColumn(
        string FkName,
        int Ordinal,
        string Column,
        string RefSchema,
        string RefTable,
        string RefColumn);
}
