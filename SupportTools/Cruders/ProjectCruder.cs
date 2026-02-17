using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliParametersDataEdit.FieldEditors;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ProjectCruder : ParCruder<ProjectModel>
{
    private const string CsProjExtension = ".csproj";
    private const string EsProjExtension = ".esproj";

    public ProjectCruder(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager,
        Dictionary<string, ProjectModel> currentValuesDictionary) : base(parametersManager, currentValuesDictionary,
        "Project", "Projects")
    {
        string[] gitProjectNamesParameterNames = new[]
        {
            nameof(ProjectModel.GitProjectNames), nameof(ProjectModel.ScaffoldSeederGitProjectNames)
        };
        FieldEditors.Add(new BoolFieldEditor(nameof(ProjectModel.IsService)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProjectGroupName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProjectDescription)));
        FieldEditors.Add(new BoolFieldEditor(nameof(ProjectModel.UseAlternativeWebAgent)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProgramArchiveDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProgramArchiveExtension)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ParametersFileDateMask)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ParametersFileExtension)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(ProjectModel.ProjectFolderName)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.SolutionFileName)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(ProjectModel.ProjectSecurityFolderPath)));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.MainProjectName),
            gitProjectNamesParameterNames, CsProjExtension, parametersManager));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.ApiContractsProjectName),
            gitProjectNamesParameterNames, CsProjExtension, parametersManager));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.SpaProjectName),
            gitProjectNamesParameterNames, EsProjExtension, parametersManager));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.AppSetEnKeysJsonFileName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.KeyGuidPart)));
        FieldEditors.Add(new DatabaseParametersFieldEditor(logger, httpClientFactory,
            nameof(ProjectModel.DevDatabaseParameters), parametersManager));
        FieldEditors.Add(new DatabaseParametersFieldEditor(logger, httpClientFactory,
            nameof(ProjectModel.ProdCopyDatabaseParameters), parametersManager));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.DbContextName)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ProjectShortPrefix)));
        FieldEditors.Add(new TextFieldEditor(nameof(ProjectModel.ScaffoldSeederProjectName)));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.DbContextProjectName),
            gitProjectNamesParameterNames, CsProjExtension, parametersManager, true));
        FieldEditors.Add(new GitProjectNameFieldEditor(nameof(ProjectModel.NewDataSeedingClassLibProjectName),
            gitProjectNamesParameterNames, CsProjExtension, parametersManager, true));
        //FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.SolutionFileNameWithMigrationProject)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.MigrationStartupProjectFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.MigrationProjectFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.SeedProjectFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.SeedProjectParametersFilePath)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.PrepareProdCopyDatabaseProjectFilePath)));
        FieldEditors.Add(
            new FilePathFieldEditor(nameof(ProjectModel.PrepareProdCopyDatabaseProjectParametersFilePath)));
        //FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.GetJsonFromScaffoldDbProjectFileFullName)));
        //FieldEditors.Add(
        //    new FilePathFieldEditor(nameof(ProjectModel.GetJsonFromScaffoldDbProjectParametersFileFullName)));
        FieldEditors.Add(new FilePathFieldEditor(nameof(ProjectModel.ExcludesRulesParametersFilePath)));
        FieldEditors.Add(new FolderPathFieldEditor(nameof(ProjectModel.MigrationSqlFilesFolder)));

        //FieldEditors.Add(new EndpointsFieldEditor(nameof(ProjectModel.Endpoints), parametersManager));
        FieldEditors.Add(new DictionaryFieldEditor<EndpointCruder, EndpointModel>(nameof(ProjectModel.Endpoints),
            parametersManager));

        //FieldEditors.Add(new RouteClassesFieldEditor(nameof(ProjectModel.RouteClasses), parametersManager));
        FieldEditors.Add(new DictionaryFieldEditor<RouteClassCruder, RouteClassModel>(
            nameof(ProjectModel.RouteClasses), parametersManager));

        FieldEditors.Add(new SimpleNamesListFieldEditor<ProjectNpmPackagesLisCruder>(
            nameof(ProjectModel.FrontNpmPackageNames), logger, httpClientFactory, parametersManager));
    }

    public static ProjectCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        return new ProjectCruder(logger, httpClientFactory, parametersManager, parameters.Projects);
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        var parameters = (SupportToolsParameters)ParametersManager.Parameters;
        Dictionary<string, ProjectModel> projects = parameters.Projects;
        ProjectModel project = projects[itemName];

        var detailsCruder = new RedundantFileNameCruder(ParametersManager, itemName);
        var newItemCommand = new NewItemCliMenuCommand(detailsCruder, itemName, $"Create New {detailsCruder.CrudName}");
        itemSubMenuSet.AddMenuItem(newItemCommand);

        foreach (ItemSubMenuCliMenuCommand detailListCommand in project.RedundantFileNames.Select(mask =>
                     new ItemSubMenuCliMenuCommand(detailsCruder, mask, itemName, true)))
        {
            itemSubMenuSet.AddMenuItem(detailListCommand);
        }
    }
}
