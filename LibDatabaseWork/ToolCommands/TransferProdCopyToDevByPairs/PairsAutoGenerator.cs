using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//pairs ფაილის ავტო-დაგენერირება — იყენებს არსებულ DbSchemaQueryHelper-ის (case-insensitive) matching-ის ლოგიკას
internal static class PairsAutoGenerator
{
    public static async ValueTask<bool> GenerateAndSave(string prodCopyConnectionString, string devConnectionString,
        string resultFileName, ILogger logger, CancellationToken cancellationToken = default)
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

        var pairedTables = new Dictionary<string, PairedTable>();
        foreach (((string SchemaLower, string TableLower) key, TableInfo prodCopyTable) in prodCopyTables)
        {
            if (!devTables.TryGetValue(key, out TableInfo? devTable))
            {
                continue;
            }

            Dictionary<string, string> devColumnLookup =
                devTable.Columns.ToDictionary(c => c.ToLowerInvariant(), c => c);

            var pairedFields = new Dictionary<string, PairedField>();
            foreach (string prodColumn in prodCopyTable.Columns)
            {
                if (devColumnLookup.TryGetValue(prodColumn.ToLowerInvariant(), out string? devColumn))
                {
                    pairedFields.Add(prodColumn, new PairedField(prodColumn, devColumn));
                }
            }

            var pairedTable = new PairedTable(prodCopyTable.SchemaName, prodCopyTable.TableName, devTable.SchemaName,
                devTable.TableName, pairedFields);
            pairedTables.Add(pairedTable.GetItemKey(), pairedTable);
        }

        var result = new PairedDbObjectsModel(pairedTables);
        var parMan = new PairedDbObjectsParametersManager(resultFileName, result);
        if (!await parMan.Save(result, null, null, cancellationToken))
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
