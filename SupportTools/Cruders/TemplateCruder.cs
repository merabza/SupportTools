using System;
using System.Collections.Generic;
using System.Linq;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.FieldEditors;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class TemplateCruder : ParCruder
{
    public TemplateCruder(ILogger logger, ParametersManager parametersManager) : base(parametersManager,
        "Project Creator Template", "Project Creator Templates")
    {
        FieldEditors.Add(new EnumFieldEditor<ESupportProjectType>(nameof(TemplateModel.SupportProjectType),
            ESupportProjectType.Console));

        FieldEditors.Add(new TextFieldEditor(nameof(TemplateModel.TestProjectName)));
        FieldEditors.Add(new TextFieldEditor(nameof(TemplateModel.TestProjectShortName)));

        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseDatabase), false));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseDbPartFolderForDatabaseProjects), false));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseMenu), false));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseHttps), false));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseReact), false));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseCarcass), false));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseIdentity), false));
        FieldEditors.Add(new BoolFieldEditor(nameof(TemplateModel.UseBackgroundTasks), false));
        FieldEditors.Add(new ReactAppTypeNameFieldEditor(logger, nameof(TemplateModel.ReactTemplateName),
            parametersManager));
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.AppProjectCreatorAllParameters?.Templates.ToDictionary(p => p.Key, p => (ItemData)p.Value) ??
               new Dictionary<string, ItemData>();
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projectTemplates = parameters.AppProjectCreatorAllParameters?.Templates;
        return projectTemplates?.ContainsKey(recordKey) ?? false;
    }

    public override void UpdateRecordWithKey(string recordName, ItemData newRecord)
    {
        var newProject = (TemplateModel)newRecord;
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        if (parameters.AppProjectCreatorAllParameters is null)
            throw new Exception(
                "parameters.AppProjectCreatorAllParameters is null in TemplateCruder.UpdateRecordWithKey");
        parameters.AppProjectCreatorAllParameters.Templates[recordName] = newProject;
    }

    protected override void AddRecordWithKey(string recordName, ItemData newRecord)
    {
        var newProject = (TemplateModel)newRecord;
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.AppProjectCreatorAllParameters ??= new AppProjectCreatorAllParameters();
        parameters.AppProjectCreatorAllParameters.Templates.Add(recordName, newProject);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        if (parameters.AppProjectCreatorAllParameters is null)
            throw new Exception(
                "parameters.AppProjectCreatorAllParameters is null in TemplateCruder.UpdateRecordWithKey");
        var projectTemplates =
            parameters.AppProjectCreatorAllParameters.Templates;
        projectTemplates.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(string recordName, ItemData? defaultItemData)
    {
        return new TemplateModel();
    }

    protected override void CheckFieldsEnables(ItemData itemData, string? lastEditedFieldName = null)
    {
        var templateModel = (TemplateModel)itemData;
        switch (templateModel.SupportProjectType)
        {
            case ESupportProjectType.Console:
                EnableFieldByName(nameof(TemplateModel.UseMenu));
                EnableFieldByName(nameof(TemplateModel.UseHttps), false);
                EnableFieldByName(nameof(TemplateModel.UseReact), false);
                EnableFieldByName(nameof(TemplateModel.UseCarcass), false);
                EnableFieldByName(nameof(TemplateModel.UseIdentity), false);
                EnableFieldByName(nameof(TemplateModel.UseBackgroundTasks), false);
                break;
            case ESupportProjectType.Api:
                EnableFieldByName(nameof(TemplateModel.UseMenu), false);
                EnableFieldByName(nameof(TemplateModel.UseHttps));
                EnableFieldByName(nameof(TemplateModel.UseReact));
                EnableFieldByName(nameof(TemplateModel.UseCarcass));
                EnableFieldByName(nameof(TemplateModel.UseIdentity));
                EnableFieldByName(nameof(TemplateModel.UseBackgroundTasks));
                break;
            case ESupportProjectType.ScaffoldSeeder:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        EnableFieldByName(nameof(TemplateModel.ReactTemplateName), templateModel.UseReact);
        EnableFieldByName(nameof(TemplateModel.UseDbPartFolderForDatabaseProjects), templateModel.UseDatabase);
    }
}