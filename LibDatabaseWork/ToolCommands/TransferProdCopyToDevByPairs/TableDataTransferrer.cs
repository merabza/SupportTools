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
        PairedTable pt, IReadOnlyList<PairedField> insertableFields, IReadOnlySet<string> identityColumns,
        int commandTimeOut, IReadOnlyList<string> primaryKeyColumns,
        string? dataSeederRulesByTableStartupProjectFilePath, string? oldDataConvertorProjectFilePath, ILogger logger,
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

        bool hasIdentity = insertableFields.Any(f => identityColumns.Contains(f.DevFieldName));

        //კონვერტორი მხოლოდ იქ მოქმედებს, სადაც ბაზის მონაცემები მონაწილეობს — OnlySeederRules-ზე დროშა უმოქმედოა
        string? convertorProjectFilePath = null;
        if (pt.UseOldDataConvertor && pt.SeedDataType != ESeedDataType.OnlySeederRules)
        {
            if (string.IsNullOrWhiteSpace(oldDataConvertorProjectFilePath))
            {
                StShared.WriteErrorLine(
                    $"OldDataConvertorForDataSeeder not specified, but UseOldDataConvertor=true for {pt.DevSchemaName}.{pt.DevTableName}",
                    true, logger);
                return 0;
            }

            convertorProjectFilePath = oldDataConvertorProjectFilePath;
        }

        //OnlyDatabase — არსებული ქცევა: ProdCopy → Dev პირდაპირი streaming; კონვერტორის შემთხვევაში ყველა მწკრივი მისგან მოდის
        if (pt.SeedDataType == ESeedDataType.OnlyDatabase)
        {
            if (convertorProjectFilePath is null)
            {
                return await TransferStreamingFromProdCopyAsync(prodCopyConnectionString, devConnectionString, pt,
                    insertableFields, hasIdentity, commandTimeOut, logger, cancellationToken);
            }

            List<Dictionary<string, object?>>? convertorData = await LoadConvertorRowsAsync(convertorProjectFilePath,
                prodCopyConnectionString, devConnectionString, pt, insertableFields, commandTimeOut, logger,
                cancellationToken);
            if (convertorData is null)
            {
                return 0;
            }

            return await BulkInsertRowsAdjustingIdentityAsync(devConnectionString, pt, insertableFields,
                identityColumns, hasIdentity, commandTimeOut, convertorData, logger, cancellationToken);
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

        //SeederRules-ის JSON-ში FK მიმართვები გასაღების სტრიქონებითაა — ჩაწერამდე Dev ბაზის ID-ებით უნდა ჩანაცვლდეს
        if (!await SeederRulesReferenceResolver.ResolveAsync(devConnectionString, pt, insertableFields, rulesData,
                commandTimeOut, logger, cancellationToken))
        {
            return 0;
        }

        //OnlySeederRules — ყველა მწკრივი წესებიდან მოდის
        if (pt.SeedDataType == ESeedDataType.OnlySeederRules)
        {
            return await BulkInsertRowsAdjustingIdentityAsync(devConnectionString, pt, insertableFields,
                identityColumns, hasIdentity, commandTimeOut, rulesData, logger, cancellationToken);
        }

        //SeederRulesHasMorePriority ან DatabaseDataHasMorePriority — ორივე წყაროდან ჩატვირთვა და Adjust;
        //კონვერტორის შემთხვევაში ბაზის მხარის მწკრივები ProdCopy-ის პირდაპირი წაკითხვის ნაცვლად კონვერტორისგან მოდის
        List<Dictionary<string, object?>>? dbData = convertorProjectFilePath is not null
            ? await LoadConvertorRowsAsync(convertorProjectFilePath, prodCopyConnectionString, devConnectionString, pt,
                insertableFields, commandTimeOut, logger, cancellationToken)
            : await ProdCopyTableReader.ReadAsync(prodCopyConnectionString, pt, insertableFields, logger,
                cancellationToken);
        if (dbData is null)
        {
            return 0;
        }

        string tableLabel = $"{pt.DevSchemaName}.{pt.DevTableName}";
        List<Dictionary<string, object?>> dataToInsert = pt.SeedDataType == ESeedDataType.SeederRulesHasMorePriority
            ? TableRowsAdjuster.Adjust(rulesData, dbData, primaryKeyColumns, tableLabel)
            : TableRowsAdjuster.Adjust(dbData, rulesData, primaryKeyColumns, tableLabel);

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

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
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

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
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

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        await using var devConn = new SqlConnection(devConnectionString);
        await devConn.OpenAsync(cancellationToken);

        // ReSharper disable once using
        using var bulkCopy = CreateBulkCopy(devConn, pt, insertableFields, hasIdentity, commandTimeOut,
            f => f.DevFieldName);

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        return bulkCopy.RowsCopied;
    }

    //კონვერტორის გაშვება და მიღებულ მწკრივებზე გასაღებიანი FK მიმართვების ამოხსნა Dev ცნობარებით
    //(ასეთი ველების გარეშე ამომხსნელი უმოქმედოა)
    private static async Task<List<Dictionary<string, object?>>?> LoadConvertorRowsAsync(
        string convertorProjectFilePath, string prodCopyConnectionString, string devConnectionString, PairedTable pt,
        IReadOnlyList<PairedField> insertableFields, int commandTimeOut, ILogger logger,
        CancellationToken cancellationToken)
    {
        List<Dictionary<string, object?>>? rows = OldDataConvertorRunner.Run(convertorProjectFilePath,
            pt.DevTableName, prodCopyConnectionString, logger);
        if (rows is null)
        {
            return null;
        }

        bool resolved = await SeederRulesReferenceResolver.ResolveAsync(devConnectionString, pt, insertableFields,
            rows, commandTimeOut, logger, cancellationToken);
        return resolved ? rows : null;
    }

    //გარე წყაროდან (SeederRules ან კონვერტორი) მიღებული მწკრივების ჩაწერა identity სვეტების კორექტირებით:
    //სვეტი, რომელსაც წყარო არცერთ მწკრივში არ ავსებს, ამოვარდება — მნიშვნელობებს Dev ბაზა დააგენერირებს
    private static async Task<long> BulkInsertRowsAdjustingIdentityAsync(string devConnectionString, PairedTable pt,
        IReadOnlyList<PairedField> insertableFields, IReadOnlySet<string> identityColumns, bool hasIdentity,
        int commandTimeOut, List<Dictionary<string, object?>> rows, ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!hasIdentity || rows.Count <= 0)
        {
            return await BulkInsertRowsAsync(devConnectionString, pt, insertableFields, hasIdentity, commandTimeOut,
                rows, cancellationToken);
        }

        List<PairedField>? adjustedFields =
            DropIdentityColumnsNotFilledInRows(insertableFields, identityColumns, rows, pt, logger);
        if (adjustedFields is null)
        {
            return 0;
        }

        bool adjustedHasIdentity = adjustedFields.Any(f => identityColumns.Contains(f.DevFieldName));
        return await BulkInsertRowsAsync(devConnectionString, pt, adjustedFields, adjustedHasIdentity, commandTimeOut,
            rows, cancellationToken);
    }

    //თუ identity სვეტს გარე წყარო (SeederRules ან კონვერტორი) არცერთ მწკრივში არ ავსებს, სვეტი ამოვარდეს ჩასაწერი
    //ველებიდან, რომ მნიშვნელობები Dev ბაზამ დააგენერიროს; ნაწილობრივ შევსებული identity სვეტი შეცდომაა — ბრუნდება null
    private static List<PairedField>? DropIdentityColumnsNotFilledInRows(
        IReadOnlyList<PairedField> insertableFields, IReadOnlySet<string> identityColumns,
        List<Dictionary<string, object?>> rows, PairedTable pt, ILogger logger)
    {
        List<PairedField> result = [.. insertableFields];
        foreach (PairedField pf in insertableFields.Where(f => identityColumns.Contains(f.DevFieldName)))
        {
            int filledCount = rows.Count(r => IsFilled(r, pf.DevFieldName));
            if (filledCount == rows.Count)
            {
                continue;
            }

            if (filledCount > 0)
            {
                StShared.WriteErrorLine(
                    $"Identity column '{pf.DevFieldName}' of {pt.DevSchemaName}.{pt.DevTableName} is filled in {filledCount} of {rows.Count} rows. Fill it in every row or in none.",
                    true, logger);
                return null;
            }

            result.Remove(pf);
        }

        return result;
    }

    //identity სვეტისთვის „შევსებულად" ითვლება მხოლოდ არანულოვანი რიცხვი — 0 სერიალიზებული default-ია და არა რეალური ID
    private static bool IsFilled(Dictionary<string, object?> row, string fieldName)
    {
        if (!row.TryGetValue(fieldName, out object? value) || value is null)
        {
            return false;
        }

        //identity სვეტი JSON-ში მთელი რიცხვია — JsonValueToClrValue მას long-ად აბრუნებს
        return value switch
        {
            long l => l != 0,
            int i => i != 0,
            _ => true
        };
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

        // ReSharper disable once using
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
