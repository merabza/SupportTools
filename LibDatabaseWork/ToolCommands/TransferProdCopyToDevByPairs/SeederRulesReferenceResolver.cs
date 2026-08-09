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
        IReadOnlyList<PairedField> insertableFields, List<Dictionary<string, object?>> rows, int commandTimeOut,
        ILogger logger, CancellationToken cancellationToken)
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

                if (!ResolveRowsForRule(pt, rule, rows, lookup, logger))
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
}
