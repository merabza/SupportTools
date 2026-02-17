using System;
using System.Collections.Generic;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Cruders;

public sealed class TemplateCruder : ParCruder<TemplateModel>
{
    private TemplateCruder(ILogger logger, IParametersManager parametersManager,
        Dictionary<string, TemplateModel> currentValuesDictionary) : base(parametersManager, currentValuesDictionary,
        "Project Creator Template", "Project Creator Templates")
    {
        FieldEditors.Add(new EnumFieldEditor<ESupportProjectType>(nameof(TemplateModel.SupportProjectType),
            ESupportProjectType.Console));

        FieldEditors.Add(new TextFieldEditor(nameof(TemplateModel.TestProjectName)));
        FieldEditors.Add(new TextFieldEditor(nameof(TemplateModel.TestProjectShortName)));
        //FieldEditors.Add(new TextFieldEditor(nameof(TemplateModel.MediatRLicenseKey)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseDatabase)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseDbPartFolderForDatabaseProjects)));
        //FieldEditors.Add(new TextFieldEditor(nameof(TemplateModel.TestDbPartProjectName)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseMenu)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseHttps)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseReact)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseCarcass)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseIdentity)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseReCounter)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseSignalR)));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseFluentValidation)));
        FieldEditors.Add(new ReactAppTypeNameFieldEditor(logger, nameof(TemplateModel.ReactTemplateName),
            parametersManager));
    }

    public static TemplateCruder Create(ILogger logger, IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        return new TemplateCruder(logger, parametersManager,
            parameters.AppProjectCreatorAllParameters?.Templates ?? new Dictionary<string, TemplateModel>());
    }

    //protected override Dictionary<string, ItemData> GetCrudersDictionary()
    //{
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    return parameters.AppProjectCreatorAllParameters?.Templates.ToDictionary(p => p.Key, p => (ItemData)p.Value) ??
    //           [];
    //}

    //public override bool ContainsRecordWithKey(string recordKey)
    //{
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    var projectTemplates = parameters.AppProjectCreatorAllParameters?.Templates;
    //    return projectTemplates?.ContainsKey(recordKey) ?? false;
    //}

    //public override void UpdateRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    var newProject = (TemplateModel)newRecord;
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    if (parameters.AppProjectCreatorAllParameters is null)
    //        throw new Exception(
    //            "parameters.AppProjectCreatorAllParameters is null in TemplateCruder.UpdateRecordWithKey");
    //    parameters.AppProjectCreatorAllParameters.Templates[recordKey] = newProject;
    //}

    //protected override void AddRecordWithKey(string recordKey, ItemData newRecord)
    //{
    //    var newProject = (TemplateModel)newRecord;
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    parameters.AppProjectCreatorAllParameters ??= new AppProjectCreatorAllParameters();
    //    parameters.AppProjectCreatorAllParameters.Templates.Add(recordKey, newProject);
    //}

    //protected override void RemoveRecordWithKey(string recordKey)
    //{
    //    var parameters = (SupportToolsParameters)ParametersManager.Parameters;
    //    if (parameters.AppProjectCreatorAllParameters is null)
    //        throw new Exception(
    //            "parameters.AppProjectCreatorAllParameters is null in TemplateCruder.UpdateRecordWithKey");
    //    var projectTemplates = parameters.AppProjectCreatorAllParameters.Templates;
    //    projectTemplates.Remove(recordKey);
    //}

    //protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    //{
    //    return new TemplateModel();
    //}

    protected override void CheckFieldsEnables(ItemData itemData, string? lastEditedFieldName = null)
    {
        var templateModel = (TemplateModel)itemData;
        EnableFieldsByProjectType(templateModel.SupportProjectType);

        //EnableFieldByName(nameof(TemplateModel.TestDbPartProjectName),
        //    templateModel.UseDbPartFolderForDatabaseProjects);
        EnableFieldByName(nameof(TemplateModel.ReactTemplateName), templateModel.UseReact);
        EnableFieldByName(nameof(TemplateModel.UseDbPartFolderForDatabaseProjects), templateModel.UseDatabase);
    }

    private void EnableFieldsByProjectType(ESupportProjectType supportProjectType)
    {
        switch (supportProjectType)
        {
            case ESupportProjectType.Console:
                EnableFieldByName(nameof(TemplateModel.UseMenu));
                EnableFieldByName(nameof(TemplateModel.UseHttps), false);
                EnableFieldByName(nameof(TemplateModel.UseReact), false);
                EnableFieldByName(nameof(TemplateModel.UseCarcass), false);
                EnableFieldByName(nameof(TemplateModel.UseIdentity), false);
                EnableFieldByName(nameof(TemplateModel.UseReCounter), false);
                EnableFieldByName(nameof(TemplateModel.UseSignalR), false);
                EnableFieldByName(nameof(TemplateModel.UseFluentValidation), false);
                break;
            case ESupportProjectType.Razor:
                EnableFieldByName(nameof(TemplateModel.UseMenu), false);
                EnableFieldByName(nameof(TemplateModel.UseHttps));
                EnableFieldByName(nameof(TemplateModel.UseReact), false);
                EnableFieldByName(nameof(TemplateModel.UseCarcass));
                EnableFieldByName(nameof(TemplateModel.UseIdentity));
                EnableFieldByName(nameof(TemplateModel.UseReCounter));
                EnableFieldByName(nameof(TemplateModel.UseSignalR));
                EnableFieldByName(nameof(TemplateModel.UseFluentValidation));
                break;
            case ESupportProjectType.Api:
                EnableFieldByName(nameof(TemplateModel.UseMenu), false);
                EnableFieldByName(nameof(TemplateModel.UseHttps));
                EnableFieldByName(nameof(TemplateModel.UseReact));
                EnableFieldByName(nameof(TemplateModel.UseCarcass));
                EnableFieldByName(nameof(TemplateModel.UseIdentity));
                EnableFieldByName(nameof(TemplateModel.UseReCounter));
                EnableFieldByName(nameof(TemplateModel.UseSignalR));
                EnableFieldByName(nameof(TemplateModel.UseFluentValidation));
                break;
            case ESupportProjectType.ScaffoldSeeder:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(supportProjectType), supportProjectType,
                    "Unsupported project type");
        }
    }
}
