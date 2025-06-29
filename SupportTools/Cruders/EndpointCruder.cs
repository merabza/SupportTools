using CliParameters;
using CliParameters.FieldEditors;
using LanguageExt.ClassInstances;
using LibParameters;
using SupportTools.Validators;
using SupportToolsData;
using SupportToolsData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SystemToolsShared;

namespace SupportTools.Cruders;

public class EndpointCruder : ParCruder
{
    private readonly ProjectModel _project;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EndpointCruder(IParametersManager parametersManager, ProjectModel project) : base(parametersManager,
        "Endpoint", "Endpoints")
    {
        _project = project;
        FieldEditors.Add(new BoolFieldEditor(nameof(EndpointEditModel.RequireAuthorization), true));
        FieldEditors.Add(new TextFieldEditor(nameof(EndpointEditModel.Base)));
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return _project.Endpoints.ToDictionary(p => p.Key, ItemData (p) => p.Value);
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new EndpointEditModel();
    }
    public override bool ContainsRecordWithKey(string recordKey)
    {
        return _project.Endpoints.ContainsKey(recordKey);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        _project.Endpoints.Remove(recordKey);
    }

    public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not EndpointEditModel endpointModel)
            throw new Exception("newRecord is null in EndpointModel");
        var adaptedModel = AdaptEndpointModel(endpointModel);
        if (adaptedModel != null)
            _project.Endpoints[recordKey] = adaptedModel;
    }

    protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    {
        if (newRecord is not EndpointEditModel endpointModel)
            throw new Exception("newRecord is null in EndpointModel");
        var adaptedModel = AdaptEndpointModel(endpointModel);
        if (adaptedModel != null)
            _project.Endpoints.Add(recordKey, adaptedModel);
    }

    private static EndpointModel? AdaptEndpointModel(EndpointEditModel endpointEditModel)
    {

        var validator = new EndpointModelValidator();

        var validateResults = validator.Validate(endpointEditModel);
        if (validateResults.IsValid)
            return new EndpointModel
            {
                Root = endpointEditModel.Root!,
                Version = endpointEditModel.Version!,
                Base = endpointEditModel.Base!,
                EndpointName = endpointEditModel.EndpointName!,
                EndpointRoute = endpointEditModel.EndpointRoute!,
                RequireAuthorization = endpointEditModel.RequireAuthorization,
                HttpMethod = endpointEditModel.HttpMethod!,
                EndpointType = EEndpointType.Command,
                ReturnType = endpointEditModel.ReturnType!,
                SendMessageToCurrentUser = false,
            };

        //StShared.WriteErrorLine(validateResults.ToString(), true, null, false);
        return null;




    }

    
    public override bool CheckValidation(ItemData item)
    {
        if (item is not EndpointEditModel endpointModel)
            throw new Exception("newRecord is null in EndpointModel");
        var validator = new EndpointModelValidator();

        var validateResults = validator.Validate(endpointModel);
        if (!validateResults.IsValid)
            StShared.WriteErrorLine(validateResults.ToString(), true, null, false);
        return validateResults.IsValid;
    }


}