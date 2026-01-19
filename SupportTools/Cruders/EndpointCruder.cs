using System.Collections.Generic;
using CliParameters;
using CliParameters.FieldEditors;
using ParametersManagement.LibParameters;
using SupportTools.Validators;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class EndpointCruder : ParCruder<EndpointModel>
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
    }

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