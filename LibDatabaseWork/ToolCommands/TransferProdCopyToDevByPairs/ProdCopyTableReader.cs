using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//ProdCopy-ის ცხრილის ჩატვირთვა Dictionary-ების სიად — Adjust ალგორითმისთვის
public static class ProdCopyTableReader
{
    public static async Task<List<Dictionary<string, object?>>?> ReadAsync(string prodCopyConnectionString,
        PairedTable pt, IReadOnlyList<PairedField> selectableFields, ILogger logger,
        CancellationToken cancellationToken)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using var prodDbm = DbManager.Create(dbKit, prodCopyConnectionString);
        if (prodDbm is null)
        {
            StShared.WriteErrorLine("Cannot create DbManager for ProdCopy database", true, logger);
            return null;
        }

        string selectList = string.Join(", ", selectableFields.Select(f => $"[{f.ProdCopyFieldName}]"));
        string selectSql = $"SELECT {selectList} FROM [{pt.ProdCopySchemaName}].[{pt.ProdCopyTableName}]";

        prodDbm.Open();
        try
        {
            var rows = new List<Dictionary<string, object?>>();
            // ReSharper disable once using
            using IDataReader reader = await prodDbm.ExecuteReaderAsync(selectSql, CommandType.Text, cancellationToken);
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (PairedField pf in selectableFields)
                {
                    object value = reader[pf.ProdCopyFieldName];
                    row[pf.DevFieldName] = value is DBNull ? null : value;
                }

                rows.Add(row);
            }

            return rows;
        }
        finally
        {
            prodDbm.Close();
        }
    }
}
