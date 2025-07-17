using System;
using System.Collections.Generic;
using CliMenu;
using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

//RouteClassesFieldEditor
public class EndpointsFieldEditor : FieldEditor<Dictionary<string, EndpointModel>>
{
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EndpointsFieldEditor(string propertyName, ParametersManager parametersManager) : base(propertyName, false,
        null, false, null, true)
    {
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        if (record is not ProjectModel project)
            throw new Exception("project is null");

        var endpointCruder = new EndpointCruder(_parametersManager, project);
        //ჩამოვტვირთოთ გიტ სერვერიდან ინფორმაცია ყველა გიტ პროექტების შესახებ და შემდეგ ეს ინფორმაცია გამოვიყენოთ სიის ჩვენებისას
        var menuSet = endpointCruder.GetListMenu();
        return menuSet;
    }

    public override string GetValueStatus(object? record)
    {
        if (record is not ProjectModel project)
            return string.Empty;
        return string.Empty;



        //var val = GetValueOrDefault(record);
        //return val?.ToString() ?? string.Empty;
    }



}