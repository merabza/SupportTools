using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SystemTools.DatabaseToolsShared;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//ერთი ცხრილის გადატანა ProdCopy → Dev; ქცევა იცვლება pt.SeedDataType-ის მიხედვით
internal static class TableDataTransferrer
{
    public static async Task<long> TransferAsync(string prodCopyConnectionString, string devConnectionString,
        PairedTable pt, IReadOnlyList<PairedField> insertableFields, bool hasIdentity, int commandTimeOut,
        IReadOnlyList<string> primaryKeyColumns, string? dataSeederRulesByTableStartupProjectFilePath, ILogger logger,
        CancellationToken cancellationToken)
    {
        //None — ცხრილი საერთოდ არ კოპირდება
        if (pt.SeedDataType == ESeedDataType.None)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Skipping {Schema}.{Table}: SeedDataType=None", pt.DevSchemaName,
                    pt.DevTableName);
            }

            return 0;
        }

        if (insertableFields.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Skipping {Schema}.{Table}: no insertable fields after filtering computed columns",
                    pt.DevSchemaName, pt.DevTableName);
            }

            return 0;
        }

        //OnlyDatabase — არსებული ქცევა: ProdCopy → Dev პირდაპირი streaming
        if (pt.SeedDataType == ESeedDataType.OnlyDatabase)
        {
            return await TransferStreamingFromProdCopyAsync(prodCopyConnectionString, devConnectionString, pt,
                insertableFields, hasIdentity, commandTimeOut, logger, cancellationToken);
        }

        //დარჩენილი სამივე ვარიანტი მოითხოვს SeederRules-ის გაშვებას
        if (string.IsNullOrWhiteSpace(dataSeederRulesByTableStartupProjectFilePath))
        {
            StShared.WriteErrorLine(
                $"DataSeederRulesByTableStartupProjectFilePath not specified, but SeedDataType={pt.SeedDataType} for {pt.DevSchemaName}.{pt.DevTableName}",
                true, logger);
            return 0;
        }

        List<Dictionary<string, object?>>? rulesData =
            SeederRulesRunner.Run(dataSeederRulesByTableStartupProjectFilePath, pt.DevTableName, logger);
        if (rulesData is null)
        {
            return 0;
        }

        List<Dictionary<string, object?>> dataToInsert;
        if (pt.SeedDataType == ESeedDataType.OnlySeederRules)
        {
            dataToInsert = rulesData;
        }
        else
        {
            //SeederRulesHasMorePriority ან DatabaseDataHasMorePriority — ორივე წყაროდან ჩატვირთვა და Adjust
            List<Dictionary<string, object?>>? dbData = await ProdCopyTableReader.ReadAsync(prodCopyConnectionString,
                pt, insertableFields, logger, cancellationToken);
            if (dbData is null)
            {
                return 0;
            }

            string tableLabel = $"{pt.DevSchemaName}.{pt.DevTableName}";
            dataToInsert = pt.SeedDataType == ESeedDataType.SeederRulesHasMorePriority
                ? TableRowsAdjuster.Adjust(rulesData, dbData, primaryKeyColumns, tableLabel)
                : TableRowsAdjuster.Adjust(dbData, rulesData, primaryKeyColumns, tableLabel);
        }

        return await BulkInsertRowsAsync(devConnectionString, pt, insertableFields, hasIdentity, commandTimeOut,
            dataToInsert, cancellationToken);
    }

    //ProdCopy → Dev streaming SqlBulkCopy-ით (არსებული ქცევა OnlyDatabase-სთვის)
    private static async Task<long> TransferStreamingFromProdCopyAsync(string prodCopyConnectionString,
        string devConnectionString, PairedTable pt, IReadOnlyList<PairedField> insertableFields, bool hasIdentity,
        int commandTimeOut, ILogger logger, CancellationToken cancellationToken)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using var prodDbm = DbManager.Create(dbKit, prodCopyConnectionString);
        if (prodDbm is null)
        {
            logger.LogError("Cannot create DbManager for ProdCopy database");
            return 0;
        }

        await using var devConn = new SqlConnection(devConnectionString);
        await devConn.OpenAsync(cancellationToken);

        // ReSharper disable once using
        using var bulkCopy = CreateBulkCopy(devConn, pt, insertableFields, hasIdentity, commandTimeOut,
            f => f.ProdCopyFieldName);

        string selectList = string.Join(", ", insertableFields.Select(f => $"[{f.ProdCopyFieldName}]"));
        string selectSql = $"SELECT {selectList} FROM [{pt.ProdCopySchemaName}].[{pt.ProdCopyTableName}]";

        prodDbm.Open();
        try
        {
            // ReSharper disable once using
            using IDataReader reader = await prodDbm.ExecuteReaderAsync(selectSql, CommandType.Text, cancellationToken);
            await bulkCopy.WriteToServerAsync(reader, cancellationToken);
            return bulkCopy.RowsCopied;
        }
        finally
        {
            prodDbm.Close();
        }
    }

    //მზა მწკრივების ჩაწერა Dev-ში DataTable-ით — SeederRules-ით მიღებული ან შერწყმული მონაცემებისთვის
    private static async Task<long> BulkInsertRowsAsync(string devConnectionString, PairedTable pt,
        IReadOnlyList<PairedField> insertableFields, bool hasIdentity, int commandTimeOut,
        List<Dictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return 0;
        }

        using var dataTable = new DataTable();
        foreach (PairedField pf in insertableFields)
        {
            dataTable.Columns.Add(pf.DevFieldName, typeof(object));
        }

        foreach (Dictionary<string, object?> row in rows)
        {
            DataRow dataRow = dataTable.NewRow();
            foreach (PairedField pf in insertableFields)
            {
                dataRow[pf.DevFieldName] = row.TryGetValue(pf.DevFieldName, out object? value) && value is not null
                    ? value
                    : DBNull.Value;
            }

            dataTable.Rows.Add(dataRow);
        }

        await using var devConn = new SqlConnection(devConnectionString);
        await devConn.OpenAsync(cancellationToken);

        // ReSharper disable once using
        using var bulkCopy = CreateBulkCopy(devConn, pt, insertableFields, hasIdentity, commandTimeOut,
            f => f.DevFieldName);

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        return bulkCopy.RowsCopied;
    }

    private static SqlBulkCopy CreateBulkCopy(SqlConnection devConn, PairedTable pt,
        IReadOnlyList<PairedField> insertableFields, bool hasIdentity, int commandTimeOut,
        Func<PairedField, string> sourceColumnNameSelector)
    {
        var options = SqlBulkCopyOptions.KeepNulls;
        if (hasIdentity)
        {
            options |= SqlBulkCopyOptions.KeepIdentity;
        }

        var bulkCopy = new SqlBulkCopy(devConn, options, null)
        {
            DestinationTableName = $"[{pt.DevSchemaName}].[{pt.DevTableName}]",
            BulkCopyTimeout = commandTimeOut,
            BatchSize = 5000,
            EnableStreaming = true
        };

        foreach (PairedField pf in insertableFields)
        {
            bulkCopy.ColumnMappings.Add(sourceColumnNameSelector(pf), pf.DevFieldName);
        }

        return bulkCopy;
    }
}
