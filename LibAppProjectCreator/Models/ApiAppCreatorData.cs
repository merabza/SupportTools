using System.Collections.Generic;
using System.IO;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class ApiAppCreatorData
{
    private ApiAppCreatorData(AppCreatorBaseData appCreatorBaseData, ProjectForCreate mainProjectData,
        string mediatRLicenseKey, bool useReact, bool useCarcass, bool useDatabase,
        bool useDbPartFolderForDatabaseProjects, bool useIdentity, bool useReCounter, bool useSignalR,
        bool useFluentValidation, ProjectForCreate databaseProjectData, ProjectForCreate dbMigrationProjectData,
        ProjectForCreate libProjectRepositoriesProjectData, ProjectForCreate repositoriesProjectData,
        ProjectForCreate frontendProjectData, string? dbPartProjectName)
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
        DatabaseProjectData = databaseProjectData;
        DbMigrationProjectData = dbMigrationProjectData;
        LibProjectRepositoriesProjectData = libProjectRepositoriesProjectData;
        RepositoriesProjectData = repositoriesProjectData;
        FrontendProjectData = frontendProjectData;
        DbPartProjectName = dbPartProjectName;
        MediatRLicenseKey = mediatRLicenseKey;
    }

    public string MediatRLicenseKey { get; }
    public bool UseReact { get; }
    public bool UseCarcass { get; }
    public bool UseDatabase { get; }
    public bool UseDbPartFolderForDatabaseProjects { get; }
    public bool UseIdentity { get; }
    public bool UseReCounter { get; }
    public bool UseSignalR { get; }
    public bool UseFluentValidation { get; }

    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate MainProjectData { get; }
    public ProjectForCreate LibProjectRepositoriesProjectData { get; }
    public ProjectForCreate RepositoriesProjectData { get; }
    public ProjectForCreate FrontendProjectData { get; }
    public string? DbPartProjectName { get; }
    public ProjectForCreate DatabaseProjectData { get; }
    public ProjectForCreate DbMigrationProjectData { get; }

    public static ApiAppCreatorData? CreateApiAppCreatorData(ILogger logger, AppCreatorBaseData appCreatorBaseData,
        string projectName, string? dbPartProjectName, TemplateModel template)
    {
        switch (template)
        {
            case { UseCarcass: true, UseDatabase: false }:
                StShared.WriteErrorLine("Use Carcass without database is not allowed", true, logger);
                return null;
            case { UseCarcass: true, UseIdentity: false }:
                StShared.WriteErrorLine("Use Carcass without Identity is not allowed", true, logger);
                return null;
        }

        if (template.UseReact && string.IsNullOrWhiteSpace(template.ReactTemplateName))
        {
            StShared.WriteErrorLine("if Use React, ReactTemplateName must be specified", true, logger);
            return null;
        }

        if (template.UseDatabase && string.IsNullOrWhiteSpace(dbPartProjectName))
        {
            StShared.WriteErrorLine("if Use Database, DbPartProjectName must be specified", true, logger);
            return null;
        }

        var projectFolders = new List<string> { "Properties", "Models", "Installers" };

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, projectName, projectName,
            EDotnetProjectType.Web, template.UseHttps ? "--no-https" : string.Empty, "Program", [.. projectFolders]);

        var libProjectRepositoriesProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"Lib{projectName}Repositories", []);

        var databaseProjectFolders = new List<string> { "Models", "Installers" };

        if (template.UseDatabase) databaseProjectFolders.Add("QueryModels");

        var currentDbPartProjectName =
            template.UseDbPartFolderForDatabaseProjects && !string.IsNullOrWhiteSpace(dbPartProjectName)
                ? dbPartProjectName
                : projectName;

        var dbPartFolderName = $"{currentDbPartProjectName}Part";

        var dbPartPath = template.UseDbPartFolderForDatabaseProjects
            ? Path.Combine(appCreatorBaseData.WorkPath, dbPartFolderName)
            : appCreatorBaseData.SolutionPath;

        var dbPartSolutionFolderName = template.UseDbPartFolderForDatabaseProjects ? dbPartFolderName : null;

        var databaseProjectData = ProjectForCreate.CreateClassLibProject(dbPartPath, currentDbPartProjectName,
            [.. databaseProjectFolders], dbPartSolutionFolderName);

        var dbMigrationProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"{currentDbPartProjectName}Migration", ["Migrations"]);

        var repositoriesProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"{currentDbPartProjectName}Repositories", ["Installers"]);

        var frontProjectFolderName = $"{projectName}Front";
        var frontEndProjectName = $"{projectName.ToLower()}frontend";
        var createInPath = Path.Combine(appCreatorBaseData.WorkPath, frontProjectFolderName);
        var frontendProjectData =
            ProjectForCreate.CreateReactProject(createInPath, frontEndProjectName, [], frontProjectFolderName);

        return new ApiAppCreatorData(appCreatorBaseData, mainProjectData, template.MediatRLicenseKey, template.UseReact, template.UseCarcass,
            template.UseDatabase, template.UseDbPartFolderForDatabaseProjects, template.UseIdentity,
            template.UseReCounter, template.UseSignalR, template.UseFluentValidation, databaseProjectData,
            dbMigrationProjectData, libProjectRepositoriesProjectData, repositoriesProjectData, frontendProjectData,
            dbPartProjectName);
    }
}