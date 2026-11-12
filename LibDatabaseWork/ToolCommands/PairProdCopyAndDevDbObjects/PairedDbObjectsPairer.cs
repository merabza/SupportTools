using System;
using System.Collections.Generic;
using System.Linq;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

//ProdCopy და Dev ბაზების ცხრილებისა და ველების ავტომატური დაწყვილება სახელებით (რეგისტრის გარეშე).
//არსებული წყვილების დაკეტილი ცხრილები და ველები უცვლელი რჩება:
//- დაკეტილი ცხრილის პარამეტრები არ იცვლება, მისი დაუკეტავი ველები კი თავიდან წყვილდება მის ცხრილებს შორის;
//- დაკეტილი ველი არ იცვლება როგორც დაკეტილ, ისე დაუკეტავ ცხრილში;
//- დაკეტილი ცხრილის ან ველის მიერ დაკავებული ცხრილი ან სვეტი ავტომატურად სხვასთან აღარ წყვილდება
public static class PairedDbObjectsPairer
{
    public static Dictionary<string, PairedTable> Pair(
        Dictionary<(string SchemaLower, string TableLower), TableInfo> prodCopyTables,
        Dictionary<(string SchemaLower, string TableLower), TableInfo> devTables, bool prodCopyUsesSchemas,
        Dictionary<string, PairedTable> existingTables, ILogger? logger)
    {
        var usedDevKeys = new HashSet<(string, string)>();
        var lockedByProdCopyKey = new Dictionary<(string, string), List<KeyValuePair<string, PairedTable>>>();
        var lockedWithoutProdCopyTable = new List<KeyValuePair<string, PairedTable>>();

        foreach (KeyValuePair<string, PairedTable> kvp in existingTables.Where(x => x.Value.Locked))
        {
            PairedTable lockedTable = kvp.Value;
            (string, string)? prodCopyKey = FindTableKey(prodCopyTables, lockedTable.ProdCopySchemaName,
                lockedTable.ProdCopyTableName, true);
            //სქემის გარეშე ProdCopy-სთვის Dev ცხრილები მხოლოდ სახელითაა გასაღებში (ReKeyDevTablesByTableNameOnly)
            (string, string)? devKey = FindTableKey(devTables, lockedTable.DevSchemaName, lockedTable.DevTableName,
                prodCopyUsesSchemas);

            if (devKey is not null)
            {
                usedDevKeys.Add(devKey.Value);
            }

            if (prodCopyKey is null || devKey is null)
            {
                StShared.WriteWarningLine(
                    $"Tables of locked paired table {kvp.Key} were not found, only its locked fields are kept", true,
                    logger);
                lockedTable.PairedFields = PairFields(lockedTable.PairedFields, [], []);
            }
            else
            {
                lockedTable.PairedFields = PairFields(lockedTable.PairedFields,
                    prodCopyTables[prodCopyKey.Value].Columns, devTables[devKey.Value].Columns);
            }

            if (prodCopyKey is null)
            {
                lockedWithoutProdCopyTable.Add(kvp);
                continue;
            }

            if (!lockedByProdCopyKey.TryGetValue(prodCopyKey.Value,
                    out List<KeyValuePair<string, PairedTable>>? lockedTables))
            {
                lockedTables = [];
                lockedByProdCopyKey.Add(prodCopyKey.Value, lockedTables);
            }

            lockedTables.Add(kvp);
        }

        //დაუკეტავი ცხრილები თავიდან იქმნება, მათგან მხოლოდ დაკეტილი ველები გადმოდის
        var unlockedByItemKey = new Dictionary<string, PairedTable>(StringComparer.OrdinalIgnoreCase);
        foreach (PairedTable unlockedTable in existingTables.Values.Where(x => !x.Locked))
        {
            unlockedByItemKey.TryAdd(unlockedTable.GetItemKey(), unlockedTable);
        }

        var reusedItemKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        //ცხრილების რიგი ProdCopy ბაზის რიგს მიყვება, რომ ფაილის ცვლილებები მცირე იყოს
        var result = new Dictionary<string, PairedTable>();
        foreach (((string SchemaLower, string TableLower) key, TableInfo prodCopyTable) in prodCopyTables)
        {
            if (lockedByProdCopyKey.TryGetValue(key, out List<KeyValuePair<string, PairedTable>>? lockedTables))
            {
                foreach (KeyValuePair<string, PairedTable> lockedTable in lockedTables)
                {
                    result.Add(lockedTable.Key, lockedTable.Value);
                }

                continue;
            }

            if (usedDevKeys.Contains(key) || !devTables.TryGetValue(key, out TableInfo? devTable))
            {
                continue;
            }

            var pairedTable = new PairedTable(prodCopyTable.SchemaName, prodCopyTable.TableName, devTable.SchemaName,
                devTable.TableName, []);
            string itemKey = pairedTable.GetItemKey();
            Dictionary<string, PairedField> oldFields = [];
            if (unlockedByItemKey.TryGetValue(itemKey, out PairedTable? oldTable))
            {
                oldFields = oldTable.PairedFields;
                reusedItemKeys.Add(itemKey);
            }

            pairedTable.PairedFields = PairFields(oldFields, prodCopyTable.Columns, devTable.Columns);
            result.Add(itemKey, pairedTable);
        }

        foreach (KeyValuePair<string, PairedTable> lockedTable in lockedWithoutProdCopyTable)
        {
            result.Add(lockedTable.Key, lockedTable.Value);
        }

        //დაუკეტავი ცხრილი, რომელიც ავტომატურად აღარ დაწყვილდა, თავის დაკეტილ ველებთან ერთად ქრება
        foreach (KeyValuePair<string, PairedTable> lostTable in existingTables.Where(x =>
                     !x.Value.Locked && !reusedItemKeys.Contains(x.Value.GetItemKey()) &&
                     x.Value.PairedFields.Values.Any(f => f.Locked)))
        {
            StShared.WriteWarningLine(
                $"Paired table {lostTable.Key} was not paired again, its locked fields are lost. Lock the table to keep it",
                true, logger);
        }

        return result;
    }

    //ველების დაწყვილება სვეტების სახელებით. დაკეტილი ველები უცვლელად რჩება და მათი სვეტები სხვასთან აღარ წყვილდება
    private static Dictionary<string, PairedField> PairFields(Dictionary<string, PairedField> oldFields,
        List<string> prodCopyColumns, List<string> devColumns)
    {
        List<KeyValuePair<string, PairedField>> lockedFields = [.. oldFields.Where(x => x.Value.Locked)];
        var usedDevColumns = new HashSet<string>(lockedFields.Select(x => x.Value.DevFieldName),
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> devColumnLookup = devColumns.ToDictionary(c => c.ToUpperInvariant(), c => c);

        //ველების რიგი ProdCopy ცხრილის სვეტების რიგს მიყვება
        var result = new Dictionary<string, PairedField>();
        foreach (string prodCopyColumn in prodCopyColumns)
        {
            List<KeyValuePair<string, PairedField>> lockedOfColumn =
            [
                .. lockedFields.Where(x =>
                    string.Equals(x.Value.ProdCopyFieldName, prodCopyColumn, StringComparison.OrdinalIgnoreCase))
            ];
            if (lockedOfColumn.Count > 0)
            {
                foreach (KeyValuePair<string, PairedField> lockedField in lockedOfColumn)
                {
                    result.Add(lockedField.Key, lockedField.Value);
                }

                continue;
            }

            if (!devColumnLookup.TryGetValue(prodCopyColumn.ToUpperInvariant(), out string? devColumn) ||
                usedDevColumns.Contains(devColumn))
            {
                continue;
            }

            var pairedField = new PairedField(prodCopyColumn, devColumn);
            result.Add(pairedField.GetItemKey(), pairedField);
        }

        //დაკეტილი ველები, რომელთა ProdCopy სვეტი აღარ არსებობს, მაინც რჩება
        foreach (KeyValuePair<string, PairedField> lockedField in lockedFields.Where(x =>
                     !result.ContainsKey(x.Key)))
        {
            result.Add(lockedField.Key, lockedField.Value);
        }

        return result;
    }

    private static (string, string)? FindTableKey(Dictionary<(string SchemaLower, string TableLower), TableInfo> tables,
        string schemaName, string tableName, bool compareSchemas)
    {
        foreach (KeyValuePair<(string SchemaLower, string TableLower), TableInfo> kvp in tables)
        {
            if (string.Equals(kvp.Value.TableName, tableName, StringComparison.OrdinalIgnoreCase) &&
                (!compareSchemas || string.Equals(kvp.Value.SchemaName, schemaName, StringComparison.OrdinalIgnoreCase)))
            {
                return kvp.Key;
            }
        }

        return null;
    }
}
