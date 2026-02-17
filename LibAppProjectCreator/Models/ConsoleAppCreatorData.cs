using System;
using System.Collections.Generic;
using LibDotnetWork;
using SupportToolsData.Models;

namespace LibAppProjectCreator.Models;

public sealed class ConsoleAppCreatorData
{
    private ConsoleAppCreatorData(AppCreatorBaseData appCreatorBaseData, ProjectForCreate mainProjectData,
        ProjectForCreate? libProjectRepositoriesProjectData, ProjectForCreate? doProjectData,
        ProjectForCreate? databaseProjectData, ProjectForCreate? dbMigrationProjectData, bool useDatabase, bool useMenu)
    {
        MainProjectData = mainProjectData;
        AppCreatorBaseData = appCreatorBaseData;
        DoProjectData = doProjectData;
        LibProjectRepositoriesProjectData = libProjectRepositoriesProjectData;
        DatabaseProjectData = databaseProjectData;
        DbMigrationProjectData = dbMigrationProjectData;
        UseDatabase = useDatabase;
        UseMenu = useMenu;
    }

    public bool UseDatabase { get; }
    public bool UseMenu { get; }

    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate MainProjectData { get; }

    public ProjectForCreate DoProjectData =>
        field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(DoProjectData));

    public ProjectForCreate LibProjectRepositoriesProjectData =>
        field ?? throw new InvalidOperationException("Uninitialized property: " +
                                                     nameof(LibProjectRepositoriesProjectData));

    public ProjectForCreate DatabaseProjectData =>
        field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(DatabaseProjectData));

    public ProjectForCreate DbMigrationProjectData =>
        field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(DbMigrationProjectData));

    public static ConsoleAppCreatorData Create(AppCreatorBaseData appCreatorBaseData, string projectName,
        TemplateModel template)
    {
        var projectFolders = new List<string> { "Properties" };
        if (!template.UseDatabase)
        {
            projectFolders.Add("Models");
        }

        if (template.UseMenu)
        {
            projectFolders.Add("MenuCommands");
        }

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, projectName, projectName,
            EDotnetProjectType.Console, string.Empty, "Program", [.. projectFolders]);

        if (!template.UseDatabase)
        {
            return new ConsoleAppCreatorData(appCreatorBaseData, mainProjectData, null, null, null, null,
                template.UseDatabase, template.UseMenu);
        }

        var libProjectRepositoriesProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"Lib{projectName}Repositories", []);

        var doProjectData =
            ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath, $"Do{projectName}", ["Models"]);
        var databaseProjectData =
            ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath, $"{projectName}Db", ["Models"]);
        var dbMigrationProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"{projectName}DbMigration", ["Migrations"]);

        return new ConsoleAppCreatorData(appCreatorBaseData, mainProjectData, libProjectRepositoriesProjectData,
            doProjectData, databaseProjectData, dbMigrationProjectData, template.UseDatabase, template.UseMenu);
    }
}
