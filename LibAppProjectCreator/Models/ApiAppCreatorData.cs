using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class ApiAppCreatorData
{
    private ApiAppCreatorData(AppCreatorBaseData appCreatorBaseData, ProjectForCreate mainProjectData, bool useReact,
        bool useCarcass, bool useDatabase, bool useDbPartFolderForDatabaseProjects, bool useIdentity, bool useReCounter,
        bool useSignalR, bool useFluentValidation, string? reactTemplateName, ProjectForCreate databaseProjectData,
        ProjectForCreate dbMigrationProjectData, ProjectForCreate libProjectRepositoriesProjectData,
        ProjectForCreate repositoriesProjectData, ProjectForCreate frontendProjectData)
    {
        AppCreatorBaseData = appCreatorBaseData;
        MainProjectData = mainProjectData;
        UseReact = useReact;
        UseCarcass = useCarcass;
        UseDatabase = useDatabase;
        UseDbPartFolderForDatabaseProjects = useDbPartFolderForDatabaseProjects;
        UseIdentity = useIdentity;
        UseReCounter = useReCounter;
        UseSignalR = useSignalR;
        UseFluentValidation = useFluentValidation;
        ReactTemplateName = reactTemplateName;
        DatabaseProjectData = databaseProjectData;
        DbMigrationProjectData = dbMigrationProjectData;
        LibProjectRepositoriesProjectData = libProjectRepositoriesProjectData;
        RepositoriesProjectData = repositoriesProjectData;
        FrontendProjectData = frontendProjectData;
    }

    public bool UseReact { get; set; }
    public bool UseCarcass { get; set; }
    public bool UseDatabase { get; set; }
    public bool UseDbPartFolderForDatabaseProjects { get; }
    public bool UseIdentity { get; set; }
    public bool UseReCounter { get; set; }
    public bool UseSignalR { get; set; }
    public bool UseFluentValidation { get; }
    public string? ReactTemplateName { get; }
    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate MainProjectData { get; }
    public ProjectForCreate LibProjectRepositoriesProjectData { get; }
    public ProjectForCreate RepositoriesProjectData { get; }
    public ProjectForCreate FrontendProjectData { get; }
    public ProjectForCreate DatabaseProjectData { get; }
    public ProjectForCreate DbMigrationProjectData { get; }


    public static ApiAppCreatorData? CreateApiAppCreatorData(ILogger logger, AppCreatorBaseData appCreatorBaseData,
        string projectName, TemplateModel template)
    {
        if (template is { UseCarcass: true, UseDatabase: false })
        {
            StShared.WriteErrorLine("Use Carcass without database is not allowed", true, logger);
            return null;
        }

        if (template is { UseCarcass: true, UseIdentity: false })
        {
            StShared.WriteErrorLine("Use Carcass without Identity is not allowed", true, logger);
            return null;
        }

        if (template.UseReact && string.IsNullOrWhiteSpace(template.ReactTemplateName))
        {
            StShared.WriteErrorLine("if Use React, ReactTemplateName must be specified", true, logger);
            return null;
        }

        var projectFolders = new List<string> { "Properties", "Models", "Installers" };

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, projectName, projectName,
            EDotnetProjectType.Web, template.UseHttps ? string.Empty : "--no-https", "Program", [.. projectFolders]);

        var libProjectRepositoriesProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"Lib{projectName}Repositories", []);

        var databaseProjectFolders = new List<string> { "Models", "Installers" };

        if (template.UseCarcass)
            databaseProjectFolders.Add("QueryModels");

        var dbPartFolderName = $"{projectName}DbPart";

        var dbPartPath = template.UseDbPartFolderForDatabaseProjects
            ? Path.Combine(appCreatorBaseData.WorkPath, dbPartFolderName)
            : appCreatorBaseData.SolutionPath;

        var dbPartSolutionFolderName = template.UseDbPartFolderForDatabaseProjects ? dbPartFolderName : null;

        var databaseProjectData = ProjectForCreate.CreateClassLibProject(dbPartPath, $"{projectName}Db",
            [.. databaseProjectFolders], dbPartSolutionFolderName);

        var dbMigrationProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"{projectName}DbMigration", ["Migrations"]);

        var repositoriesProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"{projectName}Repositories", ["Installers"]);

        var frontProjectFolderName = $"{projectName}Front";
        var frontEndProjectName = $"{projectName.ToLower()}frontend";
        var createInPath = Path.Combine(appCreatorBaseData.WorkPath, frontProjectFolderName);
        var frontendProjectData =
            ProjectForCreate.CreateReactProject(createInPath, frontEndProjectName, [], frontProjectFolderName);

        return new ApiAppCreatorData(appCreatorBaseData, mainProjectData, template.UseReact, template.UseCarcass,
            template.UseDatabase, template.UseDbPartFolderForDatabaseProjects, template.UseIdentity,
            template.UseReCounter, template.UseSignalR, template.UseFluentValidation, template.ReactTemplateName,
            databaseProjectData, dbMigrationProjectData, libProjectRepositoriesProjectData, repositoriesProjectData,
            frontendProjectData);
    }
}