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

        //თუ გასაღების ველი რომელიმე წყაროს არცერთ მწკრივში არ არის შევსებული, შერწყმა შეუძლებელია —
        //ზუსტი შეტყობინება ჯობია ცარიელი გასაღებების დუბლიკატებად გამოცხადებას
        void ThrowIfKeyFieldNeverFilled(List<Dictionary<string, object?>> list, string listLabel)
        {
            if (list.Count == 0)
            {
                return;
            }

            string? missingKeyFieldName = keyFieldNames.FirstOrDefault(k =>
                list.TrueForAll(row => !row.TryGetValue(k, out object? v) || v is null));
            if (missingKeyFieldName is not null)
            {
                throw new InvalidOperationException(
                    $"Key field '{missingKeyFieldName}' has no value in any {listLabel} row for {tableLabel}. Set KeyFieldNames for this table in the pairs file to fields both sources provide.");
            }
        }

        ThrowIfKeyFieldNeverFilled(listWithMorePriority, "priority");
        ThrowIfKeyFieldNeverFilled(listWithLessPriority, "secondary");

        string keyFieldsLabel = string.Join(", ", keyFieldNames);

        List<string> duplicatePriorityKeys =
        [
            .. listWithMorePriority.GroupBy(KeySelector).Where(g => g.Count() > 1).Select(g => g.Key)
        ];
        if (duplicatePriorityKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Priority keys contain duplicates for {tableLabel} (key fields: {keyFieldsLabel}): {string.Join(", ", duplicatePriorityKeys.Select(k => $"'{k}'"))}");
        }

        List<string> duplicateSecondaryKeys =
        [
            .. listWithLessPriority.GroupBy(KeySelector).Where(g => g.Count() > 1).Select(g => g.Key)
        ];
        if (duplicateSecondaryKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Secondary keys contain duplicates for {tableLabel} (key fields: {keyFieldsLabel}): {string.Join(", ", duplicateSecondaryKeys.Select(k => $"'{k}'"))}");
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
