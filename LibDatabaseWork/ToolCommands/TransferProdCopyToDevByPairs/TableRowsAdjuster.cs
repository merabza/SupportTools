using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//DataSeeder.cs-ის Adjust მეთოდის ანალოგი: ორი სიის შერწყმა გასაღების მიხედვით, უპირატესია listWithMorePriority
public static class TableRowsAdjuster
{
    public static List<Dictionary<string, object?>> Adjust(List<Dictionary<string, object?>> listWithMorePriority,
        List<Dictionary<string, object?>> listWithLessPriority, IReadOnlyList<string> keyFieldNames, string tableLabel)
    {
        if (keyFieldNames.Count == 0)
        {
            throw new InvalidOperationException($"Key field name list is not set for {tableLabel}");
        }

        string KeySelector(Dictionary<string, object?> row) =>
            string.Join('_',
                keyFieldNames.Select(k =>
                    !row.TryGetValue(k, out object? v) || v is null
                        ? string.Empty
                        : v.ToString()?.ToLower(CultureInfo.CurrentCulture) ?? string.Empty));

        List<string> duplicatePriorityKeys =
        [
            .. listWithMorePriority.GroupBy(KeySelector).Where(g => g.Count() > 1).Select(g => g.Key)
        ];
        if (duplicatePriorityKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Priority keys contain duplicates for {tableLabel}: {string.Join(", ", duplicatePriorityKeys)}");
        }

        List<string> duplicateSecondaryKeys =
        [
            .. listWithLessPriority.GroupBy(KeySelector).Where(g => g.Count() > 1).Select(g => g.Key)
        ];
        if (duplicateSecondaryKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Secondary keys contain duplicates for {tableLabel}: {string.Join(", ", duplicateSecondaryKeys)}");
        }

        Dictionary<string, Dictionary<string, object?>> priorDict =
            listWithMorePriority.ToDictionary(KeySelector, v => v);
        Dictionary<string, Dictionary<string, object?>> secondDict =
            listWithLessPriority.ToDictionary(KeySelector, v => v);

        var retList = new List<Dictionary<string, object?>>(priorDict.Values);
        retList.AddRange(secondDict.Where(kvp => !priorDict.ContainsKey(kvp.Key)).Select(kvp => kvp.Value));
        return retList;
    }
}
