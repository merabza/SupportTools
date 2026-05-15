using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//case-sensitive შემოწმება: pairs-ში მოცემული ცხრილი/ველი არსებობს თუ არა ბაზაში ზუსტი რეგისტრით
internal static class PairsSchemaValidator
{
    public static ValidationReport Validate(PairedDbObjectsModel pairs,
        Dictionary<(string Schema, string Table), TableInfo> schema, bool useProdCopySide, string sideName,
        ILogger logger)
    {
        var report = new ValidationReport();

        //case-insensitive lookup-ი diagnostic-ისთვის: მისახვედრად, რა სახელით არსებობს რეალურად
        var insensitiveTables = schema.Keys.ToDictionary(
            k => (k.Schema.ToLowerInvariant(), k.Table.ToLowerInvariant()), k => k);

        foreach (PairedTable pt in pairs.PairedTables)
        {
            string pairedSchema = useProdCopySide ? pt.ProdCopySchemaName : pt.DevSchemaName;
            string pairedTable = useProdCopySide ? pt.ProdCopyTableName : pt.DevTableName;
            (string Schema, string Table) key = (pairedSchema, pairedTable);

            if (!schema.TryGetValue(key, out TableInfo? tableInfo))
            {
                string suggestion = insensitiveTables.TryGetValue(
                    (pairedSchema.ToLowerInvariant(), pairedTable.ToLowerInvariant()),
                    out (string Schema, string Table) actual)
                    ? $"(no match — closest: {actual.Schema}.{actual.Table})"
                    : "(no match — table not found)";
                report.MissingTables.Add($"{pairedSchema}.{pairedTable} {suggestion}");
                continue;
            }

            //case-sensitive HashSet ცხრილის ველებზე
            var columnsExact = new HashSet<string>(tableInfo.Columns, System.StringComparer.Ordinal);
            var columnsInsensitive =
                tableInfo.Columns.ToDictionary(c => c.ToLowerInvariant(), c => c, System.StringComparer.Ordinal);

            foreach (PairedField pf in pt.PairedFields)
            {
                string pairedField = useProdCopySide ? pf.ProdCopyFieldName : pf.DevFieldName;
                if (columnsExact.Contains(pairedField))
                {
                    continue;
                }

                string fieldSuggestion = columnsInsensitive.TryGetValue(pairedField.ToLowerInvariant(),
                    out string? actualField)
                    ? $"(closest: {pairedSchema}.{pairedTable}.{actualField})"
                    : "(no match — field not found)";
                report.MissingFields.Add($"{pairedSchema}.{pairedTable}.{pairedField} {fieldSuggestion}");
            }
        }

        if (!report.IsValid)
        {
            WriteReport(sideName, report, logger);
        }

        return report;
    }

    private static void WriteReport(string sideName, ValidationReport report, ILogger logger)
    {
        var sb = new StringBuilder();
        int totalIssues = report.MissingTables.Count + report.MissingFields.Count;
        sb.AppendLine(CultureInfo.InvariantCulture, $"{sideName} schema mismatch — {totalIssues} issue(s):");
        if (report.MissingTables.Count > 0)
        {
            sb.AppendLine("  Missing tables (case-sensitive):");
            foreach (string entry in report.MissingTables)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    - {entry}");
            }
        }

        if (report.MissingFields.Count > 0)
        {
            sb.AppendLine("  Missing fields:");
            foreach (string entry in report.MissingFields)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    - {entry}");
            }
        }

        StShared.WriteErrorLine(sb.ToString(), true, logger);
    }
}
