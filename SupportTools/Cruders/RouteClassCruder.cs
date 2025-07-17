using System.Collections.Generic;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public class RouteClassCruder : ParCruder<RouteClassModel>
{
    // ReSharper disable once InconsistentNaming
    private const string api = nameof(api);

    // ReSharper disable once InconsistentNaming
    private const string v1 = nameof(v1);
    //private readonly ProjectModel _project;

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

    //public static RouteClassCruder Create()
    //{
    //    var parametersManager = ParametersManager.Create();
    //    var currentValuesDictionary = ((SupportToolsParameters)parametersManager.Parameters).RouteClasses;
    //    return new RouteClassCruder(parametersManager, currentValuesDictionary);
    //}

    //protected override Dictionary<string, ItemData> GetCrudersDictionary()
    //{
    //    return _project.RouteClasses.ToDictionary(p => p.Key, ItemData (p) => p.Value);
    //}

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new RouteClassModel { Root = api, Version = v1, Base = nameof(RouteClassModel.Base) };
    }

    //public override bool ContainsRecordWithKey(string recordKey)
    //{
    //    return _project.RouteClasses.ContainsKey(recordKey);
    //}

    //protected override void RemoveRecordWithKey(string recordKey)
    //{
    //    _project.RouteClasses.Remove(recordKey);
    //}

    //public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    if (newRecord is not RouteClassModel routeClassModel)
    //        throw new Exception("newRecord is null in RouteClassModel");
    //    //var adaptedModel = AdaptRouteClassModel(routeClassModel);
    //    //if (adaptedModel != null)
    //    _project.RouteClasses[recordKey] = routeClassModel;
    //}

    //protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    if (newRecord is not RouteClassModel routeClassModel)
    //        throw new Exception("newRecord is null in RouteClassModel");
    //    //var adaptedModel = AdaptRouteClassModel(routeClassModel);
    //    //if (adaptedModel != null)
    //    _project.RouteClasses.Add(recordKey, routeClassModel);
    //}

    //private static RouteClassModel? AdaptRouteClassModel(RouteClassModel routeClassEditModel)
    //{
    //    //var validator = new RouteClassModelValidator();

    //    var validateResults = validator.Validate(routeClassEditModel);
    //    if (validateResults.IsValid)
    //        return new RouteClassModel
    //        {
    //            Root = routeClassEditModel.Root!,
    //            Version = routeClassEditModel.Version!,
    //            Base = routeClassEditModel.Base!,
    //            //RouteClassName = routeClassEditModel.RouteClassName!,
    //            //RouteClassRoute = routeClassEditModel.RouteClassRoute!,
    //            //RequireAuthorization = routeClassEditModel.RequireAuthorization,
    //            //HttpMethod = routeClassEditModel.HttpMethod!,
    //            //RouteClassType = ERouteClassType.Command,
    //            //ReturnType = routeClassEditModel.ReturnType!,
    //            //SendMessageToCurrentUser = false
    //        };

    //    //StShared.WriteErrorLine(validateResults.ToString(), true, null, false);
    //    return null;
    //}

    //public override bool CheckValidation(ItemData item)
    //{
    //    //if (item is not RouteClassModel routeClassModel)
    //    //    throw new Exception("newRecord is null in RouteClassModel");
    //    //var validator = new RouteClassModelValidator();

    //    //var validateResults = validator.Validate(routeClassModel);
    //    //if (!validateResults.IsValid)
    //    //    StShared.WriteErrorLine(validateResults.ToString(), true, null, false);
    //    //return validateResults.IsValid;
    //    return true;
    //}
}