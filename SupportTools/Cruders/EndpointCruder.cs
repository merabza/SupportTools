using System.Collections.Generic;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.Validators;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.Cruders;

public class EndpointCruder : ParCruder<EndpointModel>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public EndpointCruder(IParametersManager parametersManager,
        Dictionary<string, EndpointModel> currentValuesDictionary) : base(parametersManager, currentValuesDictionary,
        "Endpoint", "Endpoints")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(EndpointModel.EndpointName)));
        FieldEditors.Add(new TextFieldEditor(nameof(EndpointModel.EndpointRoute)));
        FieldEditors.Add(new BoolFieldEditor(nameof(EndpointModel.RequireAuthorization), true));
        FieldEditors.Add(new EnumFieldEditor<EHttpMethod>(nameof(EndpointModel.HttpMethod), EHttpMethod.Get));
        FieldEditors.Add(new EnumFieldEditor<EEndpointType>(nameof(EndpointModel.EndpointType), EEndpointType.Query));
        FieldEditors.Add(new TextFieldEditor(nameof(EndpointModel.ReturnType)));
        FieldEditors.Add(new BoolFieldEditor(nameof(EndpointModel.SendMessageToCurrentUser)));
        /*
    public string EndpointName { get; set; }
           public string EndpointRoute { get; set; }
           public bool RequireAuthorization { get; set; }
           public string HttpMethod { get; set; }
           public EEndpointType EndpointType { get; set; }
           public string ReturnType { get; set; }
           public bool SendMessageToCurrentUser { get; set; }
         */
    }

    //private static EndpointModel? AdaptEndpointModel(EndpointEditModel endpointEditModel)
    //{
    //    var validator = new EndpointModelValidator();

    //    var validateResults = validator.Validate(endpointEditModel);
    //    if (validateResults.IsValid)
    //        return new EndpointModel
    //        {
    //            //Root = endpointEditModel.Root!,
    //            //Version = endpointEditModel.Version!,
    //            //Base = endpointEditModel.Base!,
    //            EndpointName = endpointEditModel.EndpointName!,
    //            EndpointRoute = endpointEditModel.EndpointRoute!,
    //            RequireAuthorization = endpointEditModel.RequireAuthorization,
    //            HttpMethod = endpointEditModel.HttpMethod!,
    //            EndpointType = EEndpointType.Command,
    //            ReturnType = endpointEditModel.ReturnType!,
    //            SendMessageToCurrentUser = false
    //        };

    //    //StShared.WriteErrorLine(validateResults.ToString(), true, null, false);
    //    return null;
    //}

    public override bool CheckValidation(ItemData item)
    {
        var endpointModel = GetTItem(item);
        var validator = new EndpointModelValidator();

        var validateResults = validator.Validate(endpointModel);
        if (!validateResults.IsValid)
            StShared.WriteErrorLine(validateResults.ToString(), true, null, false);
        return validateResults.IsValid;
    }
}