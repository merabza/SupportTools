using System.Collections.Generic;
using System.Linq;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//pairs ფაილის ავტო-დაგენერირება — იყენებს არსებულ DbSchemaQueryHelper-ის (case-insensitive) matching-ის ლოგიკას
internal static class PairsAutoGenerator
{
    public static bool GenerateAndSave(string prodCopyConnectionString, string devConnectionString,
        string resultFileName, ILogger logger)
    {
        Dictionary<(string SchemaLower, string TableLower), TableInfo>? prodCopyTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(prodCopyConnectionString, "ProdCopy", logger);
        if (prodCopyTables is null)
        {
            return false;
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? devTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(devConnectionString, "Dev", logger);
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
        if (!PairedDbObjectsFileLoader.Save(resultFileName, result, logger))
        {
            return false;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Auto-generated paired DB objects saved to {ResultFileName}. Paired tables: {Count}",
                resultFileName, pairedTables.Count);
        }

        return true;
    }
}
