using System.Collections.Generic;
using System.Linq;
using CliParameters;
using LibParameters;

namespace SupportTools.Cruders;

public abstract class SimpleNamesListCruder : ParCruder
{
    // ReSharper disable once ConvertToPrimaryConstructor
    protected SimpleNamesListCruder(IParametersManager parametersManager, string crudName, string crudNamePlural) :
        base(parametersManager, crudName, crudNamePlural)
    {
    }

    protected abstract List<string> GetList();

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetList().ToDictionary(k => k, ItemData (_) => new ItemData());
    }

    protected override ItemData CreateNewItem(ItemData? defaultItemData)
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