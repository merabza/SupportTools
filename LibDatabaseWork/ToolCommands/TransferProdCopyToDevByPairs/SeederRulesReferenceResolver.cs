using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//SeederRules-ის JSON-ში FK მიმართვები გასაღების სტრიქონებით მოდის (მაგ. PtIdDtKey="Roles");
//ეს კლასი მათ Dev ბაზაში უკვე ჩაწერილი ცნობარის ID-ებით ანაცვლებს (მაგ. PtId=3)
internal static class SeederRulesReferenceResolver
{
    private static readonly SeederReferenceRule[] ReferenceRules =
    [
        new("PtIdDtKey", "PtId", "DataTypes", "DtTable", "DtId"),
        new("CtIdDtKey", "CtId", "DataTypes", "DtTable", "DtId"),
        new("MenGroupIdMengKey", "MenGroupId", "MenuGroups", "MengKey", "MengId"),
        new("DtParentDataTypeIdDtKey", "DtParentDataTypeId", "DataTypes", "DtTable", "DtId"),
        new("DtManyToManyJoinParentDataTypeKey", "DtManyToManyJoinParentDataTypeId", "DataTypes", "DtTable", "DtId"),
        new("DtManyToManyJoinChildDataTypeKey", "DtManyToManyJoinChildDataTypeId", "DataTypes", "DtTable", "DtId")
    ];

    public static async Task<bool> ResolveAsync(string devConnectionString, PairedTable pt,
        IReadOnlyList<PairedField> insertableFields, List<Dictionary<string, object?>> rows,
        List<SeederSelfReference> selfReferences, int commandTimeOut, ILogger logger,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return true;
        }

        //წესი აქტიურია, თუ სამიზნე სვეტი ჩასაწერ ველებშია და მიმართვის ველი ერთ მწკრივში მაინც შევსებულია
        var insertableDevNames = new HashSet<string>(insertableFields.Select(f => f.DevFieldName),
            StringComparer.OrdinalIgnoreCase);
        List<SeederReferenceRule> activeRules =
        [
            .. ReferenceRules.Where(r => insertableDevNames.Contains(r.TargetColumnName) &&
                                         rows.Exists(row =>
                                             row.TryGetValue(r.RuleFieldName, out object? value) && value is not null))
        ];
        if (activeRules.Count == 0)
        {
            return true;
        }

        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using var devDbm = DbManager.Create(dbKit, devConnectionString, commandTimeOut);
        if (devDbm is null)
        {
            StShared.WriteErrorLine("Cannot create DbManager for Dev database", true, logger);
            return false;
        }

        devDbm.Open();
        try
        {
            //ერთი და იგივე ცნობარი (მაგ. DataTypes PtId-სა და CtId-სთვის) მხოლოდ ერთხელ ჩაიტვირთოს
            var lookupCache = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
            foreach (SeederReferenceRule rule in activeRules)
            {
                if (!lookupCache.TryGetValue(rule.LookupTableName, out Dictionary<string, int>? lookup))
                {
                    lookup = await LoadLookupAsync(devDbm, pt.DevSchemaName, rule, logger, cancellationToken);
                    if (lookup is null)
                    {
                        return false;
                    }

                    lookupCache[rule.LookupTableName] = lookup;
                }

                //ცხრილის მიმართვა საკუთარ თავზე ჩაწერამდე ვერ ამოიხსნება — ჩაწერის შემდეგ ApplySelfReferencesAsync ავსებს
                bool ok = IsSelfReference(pt, rule)
                    ? CollectSelfReferences(pt, rule, rows, lookup, selfReferences, logger)
                    : ResolveRowsForRule(pt, rule, rows, lookup, logger);
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex,
                $"Failed to resolve seeder rules references for {pt.DevSchemaName}.{pt.DevTableName}", true, logger);
            return false;
        }
        finally
        {
            devDbm.Close();
        }
    }

    //ცნობარის ჩატვირთვა Dev ბაზიდან: გასაღები → ID
    private static async Task<Dictionary<string, int>?> LoadLookupAsync(DbManager devDbm, string devSchemaName,
        SeederReferenceRule rule, ILogger logger, CancellationToken cancellationToken)
    {
        string selectSql =
            $"SELECT [{rule.LookupKeyColumnName}], [{rule.LookupIdColumnName}] FROM [{devSchemaName}].[{rule.LookupTableName}]";
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        // ReSharper disable once using
        using IDataReader reader = await devDbm.ExecuteReaderAsync(selectSql, CommandType.Text, cancellationToken);
        while (reader.Read())
        {
            //NULL ან არასტრიქონული გასაღები გამოტოვდეს
            if (reader[rule.LookupKeyColumnName] is not string key)
            {
                continue;
            }

            int id = (int)reader[rule.LookupIdColumnName];
            if (lookup.TryAdd(key, id))
            {
                continue;
            }

            StShared.WriteErrorLine(
                $"Duplicate key '{key}' in {devSchemaName}.{rule.LookupTableName}.{rule.LookupKeyColumnName} — cannot resolve references unambiguously",
                true, logger);
            return null;
        }

        return lookup;
    }

    //ერთი წესის გამოყენება ყველა მწკრივზე: row[RuleFieldName]="Roles" → row[TargetColumnName]=3
    private static bool ResolveRowsForRule(PairedTable pt, SeederReferenceRule rule,
        List<Dictionary<string, object?>> rows, Dictionary<string, int> lookup, ILogger logger)
    {
        foreach (Dictionary<string, object?> row in rows)
        {
            if (!row.TryGetValue(rule.RuleFieldName, out object? refValue) || refValue is null)
            {
                //null მიმართვა — სამიზნე სვეტი ცარიელი დარჩება, რაც nullable FK-სთვის ვალიდურია
                continue;
            }

            if (refValue is not string key)
            {
                StShared.WriteErrorLine(
                    $"Reference field '{rule.RuleFieldName}' of {pt.DevSchemaName}.{pt.DevTableName} must contain a string key, but got '{refValue}'",
                    true, logger);
                return false;
            }

            if (!lookup.TryGetValue(key, out int id))
            {
                string emptyHint = lookup.Count == 0
                    ? $" (lookup table {rule.LookupTableName} is empty — it must be transferred before {pt.DevTableName})"
                    : string.Empty;
                StShared.WriteErrorLine(
                    $"Key '{key}' from field '{rule.RuleFieldName}' of {pt.DevSchemaName}.{pt.DevTableName} not found in {pt.DevSchemaName}.{rule.LookupTableName}.{rule.LookupKeyColumnName}{emptyHint}. Make sure {rule.LookupTableName} is included in the pairs file, is transferred before {pt.DevTableName} and contains that key.",
                    true, logger);
                return false;
            }

            //ზედმეტი გასაღებიანი ველი მწკრივში რჩება — ჩაწერისას ისედაც უგულებელყოფილია
            row[rule.TargetColumnName] = id;
        }

        return true;
    }

    private static bool IsSelfReference(PairedTable pt, SeederReferenceRule rule)
    {
        return string.Equals(rule.LookupTableName, pt.DevTableName, StringComparison.OrdinalIgnoreCase);
    }

    //საკუთარ თავზე მიმართვების შეგროვება: მიმართული გასაღები ან იმავე პარტიაშია, ან უკვე Dev-შია;
    //სამიზნე სვეტი ჩაწერისას ცარიელი რჩება (ასეთი FK nullable-ია) და ჩაწერის შემდეგ ივსება
    private static bool CollectSelfReferences(PairedTable pt, SeederReferenceRule rule,
        List<Dictionary<string, object?>> rows, Dictionary<string, int> lookup,
        List<SeederSelfReference> selfReferences, ILogger logger)
    {
        var availableKeys = new HashSet<string>(lookup.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (Dictionary<string, object?> row in rows)
        {
            if (row.TryGetValue(rule.LookupKeyColumnName, out object? ownValue) && ownValue is string ownKey)
            {
                availableKeys.Add(ownKey);
            }
        }

        foreach (Dictionary<string, object?> row in rows)
        {
            if (!row.TryGetValue(rule.RuleFieldName, out object? refValue) || refValue is null)
            {
                continue;
            }

            if (refValue is not string key)
            {
                StShared.WriteErrorLine(
                    $"Reference field '{rule.RuleFieldName}' of {pt.DevSchemaName}.{pt.DevTableName} must contain a string key, but got '{refValue}'",
                    true, logger);
                return false;
            }

            if (!row.TryGetValue(rule.LookupKeyColumnName, out object? ownValue) || ownValue is not string ownKey)
            {
                StShared.WriteErrorLine(
                    $"Row of {pt.DevSchemaName}.{pt.DevTableName} with '{rule.RuleFieldName}'='{key}' has no string value in its key column '{rule.LookupKeyColumnName}', so the self-reference cannot be set after insert",
                    true, logger);
                return false;
            }

            if (!availableKeys.Contains(key))
            {
                StShared.WriteErrorLine(
                    $"Key '{key}' from field '{rule.RuleFieldName}' of {pt.DevSchemaName}.{pt.DevTableName} (row '{ownKey}') not found in the rows of {pt.DevTableName} nor in {pt.DevSchemaName}.{rule.LookupTableName}.{rule.LookupKeyColumnName}. Make sure the seeder rules of {pt.DevTableName} contain a row with that key.",
                    true, logger);
                return false;
            }

            selfReferences.Add(new SeederSelfReference(row, rule, ownKey, key));
        }

        return true;
    }

    //ჩაწერის შემდეგ საკუთარ თავზე მიმართვების შევსება; ჩაწერამდე გადაგდებული მწკრივები (უნიკალური ინდექსის კოლიზია,
    //შერწყმა) გამოტოვდება — ჩაწერილობა reference-ით დგინდება
    public static async Task ApplySelfReferencesAsync(string devConnectionString, PairedTable pt,
        List<SeederSelfReference> selfReferences, List<Dictionary<string, object?>> insertedRows, int commandTimeOut,
        ILogger logger, CancellationToken cancellationToken)
    {
        var inserted = new HashSet<Dictionary<string, object?>>(insertedRows, ReferenceEqualityComparer.Instance);
        List<SeederSelfReference> toApply = [.. selfReferences.Where(s => inserted.Contains(s.Row))];
        if (toApply.Count == 0)
        {
            return;
        }

        // ReSharper disable once using
        // ReSharper disable once DisposableConstructor
        await using var devConn = new SqlConnection(devConnectionString);
        await devConn.OpenAsync(cancellationToken);

        foreach (SeederSelfReference selfReference in toApply)
        {
            SeederReferenceRule rule = selfReference.Rule;
            string table = $"[{pt.DevSchemaName}].[{pt.DevTableName}]";
            string updateSql =
                $"UPDATE t SET t.[{rule.TargetColumnName}] = p.[{rule.LookupIdColumnName}] FROM {table} t INNER JOIN {table} p ON p.[{rule.LookupKeyColumnName}] = @ReferencedKey WHERE t.[{rule.LookupKeyColumnName}] = @OwnKey";
            // ReSharper disable once using
            // ReSharper disable once DisposableConstructor
#pragma warning disable CA2100, S2077 // სახელები წესების სტატიკური რუკიდან და pairs ფაილიდანაა, მნიშვნელობები — პარამეტრებით
            await using var command = new SqlCommand(updateSql, devConn);
#pragma warning restore CA2100, S2077
            command.CommandTimeout = commandTimeOut;
            command.Parameters.AddWithValue("@ReferencedKey", selfReference.ReferencedKey);
            command.Parameters.AddWithValue("@OwnKey", selfReference.OwnKey);
            int affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected != 1)
            {
                throw new InvalidOperationException(
                    $"Self-reference '{rule.RuleFieldName}'='{selfReference.ReferencedKey}' of {pt.DevSchemaName}.{pt.DevTableName} row '{selfReference.OwnKey}' updated {affected} rows instead of 1 — the referenced row is missing or the key {rule.LookupKeyColumnName} is not unique");
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Schema}.{Table}: {Count} self-references set after insert", pt.DevSchemaName,
                pt.DevTableName, toApply.Count);
        }
    }
}
