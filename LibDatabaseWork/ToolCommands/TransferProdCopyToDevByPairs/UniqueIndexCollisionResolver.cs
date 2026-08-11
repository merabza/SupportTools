using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//ჩასაწერი მწკრივების შემოწმება Dev ცხრილის უნიკალური ინდექსების მიმართ bulk insert-ამდე:
//კოლიზიისას priority წყაროს მწკრივი რჩება და secondary ვარდება warning-ით; თუ გამარჯვებულის
//არჩევა შეუძლებელია (მწკრივები ერთი წყაროდანაა), ვრცელი შეცდომა ნედლი SqlException-ის ნაცვლად
internal static class UniqueIndexCollisionResolver
{
    public static List<Dictionary<string, object?>> Resolve(List<Dictionary<string, object?>> rows,
        IReadOnlyList<PairedField> insertableFields, IReadOnlySet<string> identityColumns,
        IReadOnlyList<UniqueIndexMeta> uniqueIndexes, IReadOnlyList<string> keyFieldNames, string tableLabel,
        ISet<Dictionary<string, object?>>? priorityRows, ILogger logger)
    {
        if (rows.Count == 0 || uniqueIndexes.Count == 0)
        {
            return rows;
        }

        var insertableNames = new HashSet<string>(insertableFields.Select(f => f.DevFieldName),
            StringComparer.OrdinalIgnoreCase);
        var droppedRows = new HashSet<Dictionary<string, object?>>(ReferenceEqualityComparer.Instance);
        var errors = new List<string>();

        //ინდექსები მუშავდება მიმდევრობით — ერთ ინდექსზე გადაგდებული მწკრივი შემდეგის დაჯგუფებაში აღარ მონაწილეობს
        foreach (UniqueIndexMeta index in uniqueIndexes)
        {
            List<Dictionary<string, object?>> currentRows =
                droppedRows.Count == 0 ? rows : [.. rows.Where(r => !droppedRows.Contains(r))];
            if (!IsCheckable(index, insertableNames, identityColumns, currentRows))
            {
                continue;
            }

            ResolveIndex(index, currentRows, priorityRows, identityColumns, droppedRows, keyFieldNames, tableLabel,
                errors, logger);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unresolvable unique index collisions for {tableLabel}:\n{string.Join("\n", errors)}\nFix the source data or KeyFieldNames for this table so these rows merge.");
        }

        return droppedRows.Count == 0 ? rows : [.. rows.Where(r => !droppedRows.Contains(r))];
    }

    //ინდექსი შემოწმებადია, თუ ყველა სვეტი ჩასაწერ ველებშია და identity სვეტები ყველა მწკრივში შევსებულია —
    //სხვა შემთხვევაში მნიშვნელობებს ბაზა ან backfill მიანიჭებს და დუბლიკატები ვერ იქნება
    private static bool IsCheckable(UniqueIndexMeta index, IReadOnlySet<string> insertableNames,
        IReadOnlySet<string> identityColumns, List<Dictionary<string, object?>> currentRows)
    {
        if (!index.Columns.TrueForAll(insertableNames.Contains))
        {
            return false;
        }

        return !index.Columns.Exists(c =>
            identityColumns.Contains(c) && !currentRows.TrueForAll(r => TableDataTransferrer.IsFilled(r, c)));
    }

    private static void ResolveIndex(UniqueIndexMeta index, List<Dictionary<string, object?>> currentRows,
        ISet<Dictionary<string, object?>>? priorityRows, IReadOnlySet<string> identityColumns,
        HashSet<Dictionary<string, object?>> droppedRows, IReadOnlyList<string> keyFieldNames, string tableLabel,
        List<string> errors, ILogger logger)
    {
        Dictionary<string, List<Dictionary<string, object?>>> groups = GroupByIndexValue(index, currentRows);

        foreach (List<Dictionary<string, object?>> group in groups.Values.Where(g => g.Count > 1))
        {
            List<Dictionary<string, object?>> priorityMembers =
                priorityRows is null ? [] : [.. group.Where(priorityRows.Contains)];

            if (priorityRows is not null && priorityMembers.Count == 1)
            {
                DropSecondaryRows(index, group, priorityMembers[0], identityColumns, droppedRows, keyFieldNames,
                    tableLabel, logger);
                continue;
            }

            errors.Add(DescribeCollision(index, group, priorityRows, priorityMembers.Count, keyFieldNames));
        }
    }

    private static Dictionary<string, List<Dictionary<string, object?>>> GroupByIndexValue(UniqueIndexMeta index,
        List<Dictionary<string, object?>> currentRows)
    {
        var groups = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.Ordinal);
        foreach (Dictionary<string, object?> row in currentRows)
        {
            //ფილტრიანი ინდექსი (როგორც წესი, IS NOT NULL ფილტრით) NULL-იან მწკრივს არ ფარავს;
            //უფილტროში SQL Server-ის სემანტიკით NULL-ები ერთმანეთს ეჯახება
            if (index.HasFilter && index.Columns.Exists(c => GetValue(row, c) is null))
            {
                continue;
            }

            string groupKey = string.Join('\u001f', index.Columns.Select(c => ValueToKeyString(GetValue(row, c))));
            if (!groups.TryGetValue(groupKey, out List<Dictionary<string, object?>>? group))
            {
                group = [];
                groups[groupKey] = group;
            }

            group.Add(row);
        }

        return groups;
    }

    private static void DropSecondaryRows(UniqueIndexMeta index, List<Dictionary<string, object?>> group,
        Dictionary<string, object?> keptRow, IReadOnlySet<string> identityColumns,
        HashSet<Dictionary<string, object?>> droppedRows, IReadOnlyList<string> keyFieldNames, string tableLabel,
        ILogger logger)
    {
        string valueLabel = DescribeIndexValue(index, keptRow);
        foreach (Dictionary<string, object?> row in group.Where(r => !ReferenceEquals(r, keptRow)))
        {
            droppedRows.Add(row);
            if (!logger.IsEnabled(LogLevel.Warning))
            {
                continue;
            }

            string droppedIdentity = string.Join(", ",
                identityColumns.Where(c => TableDataTransferrer.IsFilled(row, c))
                    .Select(c => $"{c}={ValueToDisplayString(GetValue(row, c))}"));
            logger.LogWarning(
                "Dropping secondary row from {Table}: unique index {IndexName} value {Value} collides with a priority row (kept key: '{KeptKey}'). Dropped row key: '{DroppedKey}' ({DroppedIdentity}). Rows referencing the dropped row will fail FK checks if they are transferred too.",
                tableLabel, index.IndexName, valueLabel, DisplayKey(keptRow, keyFieldNames),
                DisplayKey(row, keyFieldNames),
                droppedIdentity.Length == 0 ? "no identity value" : droppedIdentity);
        }
    }

    private static string DescribeCollision(UniqueIndexMeta index, List<Dictionary<string, object?>> group,
        ISet<Dictionary<string, object?>>? priorityRows, int priorityCount, IReadOnlyList<string> keyFieldNames)
    {
        string sourceLabel = priorityRows is null
            ? "single-source data"
            : $"{priorityCount} of them from the priority source";
        string keys = string.Join(" / ", group.Select(r => $"'{DisplayKey(r, keyFieldNames)}'"));
        return
            $"  {index.IndexName} ({string.Join(", ", index.Columns)}) value {DescribeIndexValue(index, group[0])} — {group.Count} rows ({sourceLabel}), keys: {keys}";
    }

    private static string DescribeIndexValue(UniqueIndexMeta index, Dictionary<string, object?> row)
    {
        return string.Join(", ", index.Columns.Select(c => $"{c}='{ValueToDisplayString(GetValue(row, c))}'"));
    }

    private static string DisplayKey(Dictionary<string, object?> row, IReadOnlyList<string> keyFieldNames)
    {
        return string.Join('_', keyFieldNames.Select(k => ValueToDisplayString(GetValue(row, k))));
    }

    private static object? GetValue(Dictionary<string, object?> row, string fieldName)
    {
        return row.TryGetValue(fieldName, out object? value) ? value : null;
    }

    //TableRowsAdjuster.KeySelector-ის კონვენციით — string-ად და lower-ქეისში; byte[] ცალკე მუშავდება, რომ
    //"System.Byte[]" ტექსტმა სხვადასხვა მნიშვნელობები ერთმანეთს არ შეაჯახოს
    private static string ValueToKeyString(object? value)
    {
        return value switch
        {
            null => "\u001e<NULL>",
            byte[] bytes => Convert.ToBase64String(bytes),
            _ => value.ToString()?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty
        };
    }

    private static string ValueToDisplayString(object? value)
    {
        return value switch
        {
            null => "<NULL>",
            byte[] bytes => Convert.ToBase64String(bytes),
            _ => value.ToString() ?? string.Empty
        };
    }
}
