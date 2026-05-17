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
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//ერთი ცხრილის გადატანა ProdCopy → Dev SqlBulkCopy-ით; ColumnMappings ფიქსირდება pairs-დან
internal static class TableDataTransferrer
{
    public static async Task<long> TransferAsync(string prodCopyConnectionString, string devConnectionString,
        PairedTable pt, IReadOnlyList<PairedField> insertableFields, bool hasIdentity, int commandTimeOut,
        ILogger logger, CancellationToken cancellationToken)
    {
        if (insertableFields.Count == 0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Skipping {Schema}.{Table}: no insertable fields after filtering computed columns",
                    pt.DevSchemaName, pt.DevTableName);
            }

            return 0;
        }

        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using var prodDbm = DbManager.Create(dbKit, prodCopyConnectionString);
        if (prodDbm is null)
        {
            logger.LogError("Cannot create DbManager for ProdCopy database");
            return 0;
        }

        var options = SqlBulkCopyOptions.KeepNulls;
        if (hasIdentity)
        {
            options |= SqlBulkCopyOptions.KeepIdentity;
        }

        // ReSharper disable once using
        await using var devConn = new SqlConnection(devConnectionString);
        await devConn.OpenAsync(cancellationToken);

        // ReSharper disable once using
        using var bulkCopy = new SqlBulkCopy(devConn, options, null)
        {
            DestinationTableName = $"[{pt.DevSchemaName}].[{pt.DevTableName}]",
            BulkCopyTimeout = commandTimeOut,
            BatchSize = 5000,
            EnableStreaming = true
        };

        foreach (PairedField pf in insertableFields)
        {
            bulkCopy.ColumnMappings.Add(pf.ProdCopyFieldName, pf.DevFieldName);
        }

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
}
