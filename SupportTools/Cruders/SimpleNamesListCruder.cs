using System.Collections.Generic;
using System.Linq;
using CliParameters;
using LibParameters;

namespace SupportTools.Cruders;

public abstract class SimpleNamesListCruder : Cruder
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected SimpleNamesListCruder(string crudName, string crudNamePlural, bool fieldKeyFromItem = false,
        bool canEditFieldsInSequence = true) : base(crudName, crudNamePlural, fieldKeyFromItem, canEditFieldsInSequence)
    {
    }

    protected abstract List<string> GetList();

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetList().ToDictionary(k => k, ItemData (v) => new TextItemData { Text = v });
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var reactAppTemplateNames = GetList();
        return reactAppTemplateNames.Contains(recordKey);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var reactAppTemplateNames = GetList();
        reactAppTemplateNames.Remove(recordKey);
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        GetList().Add(recordKey);
    }

    public override string? GetStatusFor(string name)
    {
        return null;
    }
}