//using System;
//using System.Collections.Generic;
//using CliMenu;
//using CliParameters.FieldEditors;
//using LibParameters;
//using SupportTools.Cruders;
//using SupportToolsData.Models;

//namespace SupportTools.FieldEditors;

//public sealed class RouteClassesFieldEditor : FieldEditor<Dictionary<string, RouteClassModel>>
//{
//    private readonly ParametersManager _parametersManager;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public RouteClassesFieldEditor(string propertyName, ParametersManager parametersManager) : base(propertyName, false,
//        null, false, null, true)
//    {
//        _parametersManager = parametersManager;
//    }

//    public override CliMenuSet GetSubMenu(object record)
//    {
//        if (record is not ProjectModel project)
//            throw new Exception("project is null");

//        var routeClassCruder = new RouteClassCruder(_parametersManager, project.RouteClasses);
//        //ჩამოვტვირთოთ გიტ სერვერიდან ინფორმაცია ყველა გიტ პროექტების შესახებ და შემდეგ ეს ინფორმაცია გამოვიყენოთ სიის ჩვენებისას
//        var menuSet = routeClassCruder.GetListMenu();
//        return menuSet;
//    }
//}


