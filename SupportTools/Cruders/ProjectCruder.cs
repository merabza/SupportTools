using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using CliParameters.MenuCommands;
using CliParametersDataEdit.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ProjectCruder : ParCruder
{
    private const string CsProjExtension = ".csproj";
    private const string EsProjExtension = ".esproj";

    public ProjectCruder(ILogger logger, ParametersManager parametersManager) : base(parametersManager, "Project",
        "Projects")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ServiceName)));
        FieldEditors.Add(new BoolFieldEditor(nameof(ProjectModel.UseAlternativeWebAgent), false));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProgramArchiveDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProgramArchiveExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ParametersFileDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ParametersFileExtension)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(ProjectModel.ProjectFolderName)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.SolutionFileName)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(ProjectModel.ProjectSecurityFolderPath)));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.MainProjectName),
            nameof(ProjectModel.GitProjectNames), CsProjExtension, parametersManager));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.SpaProjectName),
            nameof(ProjectModel.GitProjectNames), EsProjExtension, parametersManager));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.AppSetEnKeysJsonFileName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.KeyGuidPart)));
        FieldEditors.Add(new DatabaseConnectionParametersFieldEditor(logger,
            nameof(ProjectModel.DevDatabaseConnectionParameters), parametersManager));
        FieldEditors.Add(new DatabaseConnectionParametersFieldEditor(logger,
            nameof(ProjectModel.ProdCopyDatabaseConnectionParameters), parametersManager));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.DbContextName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProjectShortPrefix)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ScaffoldSeederProjectName)));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.DbContextProjectName),
            nameof(ProjectModel.GitProjectNames), CsProjExtension, parametersManager));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.NewDataSeedingClassLibProjectName),
            nameof(ProjectModel.GitProjectNames), CsProjExtension, parametersManager));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.MigrationStartupProjectFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.MigrationProjectFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.SeedProjectFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.SeedProjectParametersFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.GetJsonFromScaffoldDbProjectFileFullName)));
        FieldEditors.Add(
            new FilePathFieldEditor(nameof(ProjectModel.GetJsonFromScaffoldDbProjectParametersFileFullName)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.ExcludesRulesParametersFilePath)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(ProjectModel.MigrationSqlFilesFolder)));

        //ProjectModel.RedundantFileNames აქ არ არის ჩასმული სპეციალურად, რადგან სხვა პარამეტრებთან ერთად გაშლილად გამოვა სიის სახით როგორც მენიუს დამატებითი ელემენტები
        //ProjectModel.ServerInfos აქ არ არის ჩასმული სპეციალურად, რადგან მისი რედაქტირება ხდება იქ სადაც ამოცანების შედგენა ხდება
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        return parameters.Projects.ToDictionary(p => p.Key, p => (ItemData)p.Value);
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projects = parameters.Projects;
        return projects.ContainsKey(recordKey);
    }

    public override void UpdateRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not ProjectModel newProject)
            throw new Exception("newProject is null in ProjectCruder.UpdateRecordWithKey");

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Projects[recordName] = newProject;
    }

    protected override void AddRecordWithKey(string recordName, ItemData newRecord)
    {
        if (newRecord is not ProjectModel newProject)
            throw new Exception("newProject is null in ProjectCruder.AddRecordWithKey");

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        parameters.Projects.Add(recordName, newProject);
    }

    protected override void RemoveRecordWithKey(string recordKey)
    {
        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projects = parameters.Projects;
        projects.Remove(recordKey);
    }

    protected override ItemData CreateNewItem(string recordName, ItemData? defaultItemData)
    {
        return new ProjectModel();
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordName);

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        var projects = parameters.Projects;
        var project = projects[recordName];

        RedundantFileNameCruder detailsCruder = new(ParametersManager, recordName);
        NewItemCommand newItemCommand = new(detailsCruder, recordName, $"Create New {detailsCruder.CrudName}");
        itemSubMenuSet.AddMenuItem(newItemCommand);

        foreach (var detailListCommand in project.RedundantFileNames.Select(mask =>
                     new ItemSubMenuCommand(detailsCruder, mask, recordName, true)))
            itemSubMenuSet.AddMenuItem(detailListCommand);
    }
}