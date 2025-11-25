using System.Collections.Generic;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class RouteClassCruder : ParCruder<RouteClassModel>
{
    // ReSharper disable once InconsistentNaming
    private const string api = nameof(api);

    // ReSharper disable once InconsistentNaming
    private const string v1 = nameof(v1);

    // ReSharper disable once ConvertToPrimaryConstructor
    public RouteClassCruder(IParametersManager parametersManager,
        Dictionary<string, RouteClassModel> currentValuesDictionary) : base(parametersManager, currentValuesDictionary,
        "RouteClass", "RouteClasses")
    {
        //_project = project;
        FieldEditors.Add(new TextFieldEditor(nameof(RouteClassModel.Root), api));
        FieldEditors.Add(new TextFieldEditor(nameof(RouteClassModel.Version), v1));
        FieldEditors.Add(new TextFieldEditor(nameof(RouteClassModel.Base)));
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new RouteClassModel { Root = api, Version = v1, Base = nameof(RouteClassModel.Base) };
    }
}