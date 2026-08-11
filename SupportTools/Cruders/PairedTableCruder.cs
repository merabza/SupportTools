using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SystemTools.DatabaseToolsShared;
using SystemTools.SystemToolsShared;

namespace SupportTools.Cruders;

public sealed class PairedTableCruder : ParCruder<PairedTable>
{
    private readonly Dictionary<string, PairedTable> _pairedTables;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedTableCruder(IParametersManager parametersManager,
        Dictionary<string, PairedTable> currentValuesDictionary, ILogger logger, string prodCopyConnectionString,
        string devConnectionString) : base(parametersManager, currentValuesDictionary, "Paired Table", "Paired Tables",
        true)
    {
        _pairedTables = currentValuesDictionary;
        FieldEditors.Add(new SchemaNameFieldEditor(nameof(PairedTable.ProdCopySchemaName), logger, "ProdCopy",
            prodCopyConnectionString));
        FieldEditors.Add(new TableNameFieldEditor(nameof(PairedTable.ProdCopyTableName), logger, "ProdCopy",
            prodCopyConnectionString));
        FieldEditors.Add(new SchemaNameFieldEditor(nameof(PairedTable.DevSchemaName), logger, "Dev",
            devConnectionString));
        FieldEditors.Add(new TableNameFieldEditor(nameof(PairedTable.DevTableName), logger, "Dev",
            devConnectionString));
        FieldEditors.Add(new EnumFieldEditor<ESeedDataType>(nameof(PairedTable.SeedDataType),
            ESeedDataType.OnlyDatabase));
        FieldEditors.Add(new BoolFieldEditor(nameof(PairedTable.UseOldDataConvertor)));
        FieldEditors.Add(new PairedFieldsListFieldEditor(nameof(PairedTable.PairedFields), logger, parametersManager,
            prodCopyConnectionString, devConnectionString));
    }

    public static PairedTableCruder Create(IParametersManager parametersManager, ILogger logger,
        Dictionary<string, PairedTable> tables, string prodCopyConnectionString, string devConnectionString)
    {
        return new PairedTableCruder(parametersManager, tables, logger, prodCopyConnectionString, devConnectionString);
    }

    protected override void BeforeGetListMenu()
    {
        //ძველი ფორმატის (GUID ან მხოლოდ ProdCopy მხარის) გასაღებების გადაყვანა ახალ ფორმატზე,
        //რომ სიაში ყოველთვის შედგენილი სახელი გამოჩნდეს
        List<KeyValuePair<string, PairedTable>> mismatched =
            [.. _pairedTables.Where(w => w.Key != w.Value.GetItemKey())];

        foreach (KeyValuePair<string, PairedTable> kvp in mismatched)
        {
            string newKey = CreateUniqueKey(kvp.Value, kvp.Key);
            if (newKey == kvp.Key)
            {
                continue;
            }

            _pairedTables.Remove(kvp.Key);
            _pairedTables.Add(newKey, kvp.Value);
        }
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //ახალი ჩანაწერის გასაღები თვითონ ჩანაწერის ველებიდან აიგება GUID-ის ნაცვლად
        return base.AddRecordWithKey(CreateUniqueKey(GetTItem(newRecord)), newRecord, cancellationToken);
    }

    //აგებს უნიკალურ გასაღებს ჩანაწერის ველებიდან. კოლიზიის შემთხვევაში ემატება რიგითი ნომერი
    private string CreateUniqueKey(PairedTable pairedTable, string? currentKey = null)
    {
        string itemKey = pairedTable.GetItemKey();
        string candidate = itemKey;
        var counter = 2;
        while (candidate != currentKey && _pairedTables.ContainsKey(candidate))
        {
            candidate = $"{itemKey} ({counter})";
            counter++;
        }

        return candidate;
    }
}
