using System;
using System.Collections.Generic;
using System.Linq;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;

namespace SupportTools.Cruders;

public abstract class SimpleNamesWithDescriptionsCruder : ParCruder
{
    protected SimpleNamesWithDescriptionsCruder(IParametersManager parametersManager, string crudName,
        string crudNamePlural, string descriptionFieldRealName = "Description") : base(parametersManager, crudName,
        crudNamePlural)
    {
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(TextItemData.Text), true, descriptionFieldRealName));
    }

    protected abstract Dictionary<string, string> GetDictionary();

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return GetDictionary().ToDictionary(k => k.Key, ItemData (v) => new TextItemData { Text = v.Value });
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new TextItemData();
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var reactAppTemplateNames = GetDictionary();
        return reactAppTemplateNames.ContainsKey(recordKey);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var reactAppTemplateNames = GetDictionary();
        reactAppTemplateNames.Remove(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newTextItemData)
            throw new Exception("newRecord is null in UpdateRecordWithKey");
        if (string.IsNullOrWhiteSpace(newTextItemData.Text))
            throw new Exception("newReactAppType.Description is empty in UpdateRecordWithKey");
        GetDictionary()[recordKey] = newTextItemData.Text;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not TextItemData newTextItemData)
            throw new Exception("newRecord is null in AddRecordWithKey");
        if (string.IsNullOrWhiteSpace(newTextItemData.Text))
            throw new Exception("Description is empty in AddRecordWithKey");
        GetDictionary().Add(recordKey, newTextItemData.Text);
    }

    public override string? GetStatusFor(string name)
    {
        if (GetDictionary().TryGetValue(name, out var description) && !string.IsNullOrWhiteSpace(description))
            return description;
        return null;
    }
}