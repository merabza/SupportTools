using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SystemTools.SystemToolsShared;

namespace SupportTools.Cruders;

public sealed class PairedFieldCruder : ParCruder<PairedField>
{
    private readonly Dictionary<string, PairedField> _pairedFields;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedFieldCruder(IParametersManager parametersManager,
        Dictionary<string, PairedField> currentValuesDictionary, ILogger logger, string prodCopyConnectionString,
        string devConnectionString, string prodCopySchemaName, string prodCopyTableName, string devSchemaName,
        string devTableName) : base(parametersManager, currentValuesDictionary, "Paired Field", "Paired Fields", true)
    {
        _pairedFields = currentValuesDictionary;
        FieldEditors.Add(new FieldNameFieldEditor(nameof(PairedField.ProdCopyFieldName), logger, "ProdCopy",
            prodCopyConnectionString, prodCopySchemaName, prodCopyTableName));
        FieldEditors.Add(new FieldNameFieldEditor(nameof(PairedField.DevFieldName), logger, "Dev", devConnectionString,
            devSchemaName, devTableName));
    }

    protected override void BeforeGetListMenu()
    {
        //ძველი ფორმატის (GUID ან მხოლოდ ProdCopy მხარის) გასაღებების გადაყვანა ახალ ფორმატზე,
        //რომ სიაში ყოველთვის შედგენილი სახელი გამოჩნდეს
        List<KeyValuePair<string, PairedField>> mismatched =
            [.. _pairedFields.Where(w => w.Key != w.Value.GetItemKey())];

        foreach (KeyValuePair<string, PairedField> kvp in mismatched)
        {
            string newKey = CreateUniqueKey(kvp.Value, kvp.Key);
            if (newKey == kvp.Key)
            {
                continue;
            }

            _pairedFields.Remove(kvp.Key);
            _pairedFields.Add(newKey, kvp.Value);
        }
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //ახალი ჩანაწერის გასაღები თვითონ ჩანაწერის ველებიდან აიგება GUID-ის ნაცვლად
        return base.AddRecordWithKey(CreateUniqueKey(GetTItem(newRecord)), newRecord, cancellationToken);
    }

    //აგებს უნიკალურ გასაღებს ჩანაწერის ველებიდან. კოლიზიის შემთხვევაში ემატება რიგითი ნომერი
    private string CreateUniqueKey(PairedField pairedField, string? currentKey = null)
    {
        string itemKey = pairedField.GetItemKey();
        string candidate = itemKey;
        var counter = 2;
        while (candidate != currentKey && _pairedFields.ContainsKey(candidate))
        {
            candidate = $"{itemKey} ({counter})";
            counter++;
        }

        return candidate;
    }
}
