using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibSupportToolsServerWork.Registry.Sync;

namespace SupportTools.Menu.SyncRegistry;

//გეგმის შეჯამება კოლექციების მიხედვით (C5): რამდენი ჩანაწერი ჩამოვა (Pull), წავა (Push), კონფლიქტშია, უკვე ერთნაირია
//(InSync) და გამორიცხულია (Skipped). კოლექციები გეგმის რიგითაა (Order), ბოლოს ჯამია. ჩანაწერების სახელები და
//მნიშვნელობები აქ არ ჩანს
internal static class RegistrySyncPlanSummary
{
    private const int CollectionWidth = 26;

    public static List<string> CreateLines(RegistrySyncPlan plan)
    {
        List<string> lines = [FormatLine("Collection", "Pull", "Push", "Conflict", "InSync", "Skipped")];
        lines.AddRange(plan.Items.GroupBy(x => x.CollectionName, StringComparer.OrdinalIgnoreCase)
            .Select(x => FormatCounts(x.Key, [.. x])));
        lines.Add(FormatCounts("Total", [.. plan.Items]));
        return lines;
    }

    private static string FormatCounts(string name, List<RegistrySyncPlanItem> items)
    {
        return FormatLine(name, Count(items, ERegistrySyncAction.Pull), Count(items, ERegistrySyncAction.Push),
            Count(items, ERegistrySyncAction.Conflict), Count(items, ERegistrySyncAction.InSync),
            Count(items, ERegistrySyncAction.Skipped));
    }

    private static string Count(List<RegistrySyncPlanItem> items, ERegistrySyncAction action)
    {
        return items.Count(x => x.Action == action).ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatLine(string name, string pull, string push, string conflict, string inSync,
        string skipped)
    {
        return $"{name,-CollectionWidth}{pull,6}{push,6}{conflict,10}{inSync,8}{skipped,9}";
    }
}
