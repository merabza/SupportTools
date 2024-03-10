using System;
using System.Collections.Generic;
using SupportToolsData;
using SupportToolsData.Models;

namespace LibAppProjectCreator.Models;

public sealed class ConsoleAppCreatorData
{
    private readonly ProjectForCreate? _databaseProjectData;
    private readonly ProjectForCreate? _dbMigrationProjectData;

    private readonly ProjectForCreate? _doProjectData;
    private readonly ProjectForCreate? _libProjectRepositoriesProjectData;

    private ConsoleAppCreatorData(AppCreatorBaseData appCreatorBaseData, ProjectForCreate mainProjectData,
        ProjectForCreate? libProjectRepositoriesProjectData, ProjectForCreate? doProjectData,
        ProjectForCreate? databaseProjectData, ProjectForCreate? dbMigrationProjectData, bool useDatabase, bool useMenu)
    {
        MainProjectData = mainProjectData;
        AppCreatorBaseData = appCreatorBaseData;
        _doProjectData = doProjectData;
        _libProjectRepositoriesProjectData = libProjectRepositoriesProjectData;
        _databaseProjectData = databaseProjectData;
        _dbMigrationProjectData = dbMigrationProjectData;
        UseDatabase = useDatabase;
        UseMenu = useMenu;
    }

    public bool UseDatabase { get; }
    public bool UseMenu { get; }

    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate MainProjectData { get; }

    public ProjectForCreate DoProjectData => _doProjectData ??
                                             throw new InvalidOperationException("Uninitialized property: " +
                                                 nameof(DoProjectData));

    public ProjectForCreate LibProjectRepositoriesProjectData => _libProjectRepositoriesProjectData ??
                                                                 throw new InvalidOperationException(
                                                                     "Uninitialized property: " +
                                                                     nameof(LibProjectRepositoriesProjectData));

    public ProjectForCreate DatabaseProjectData => _databaseProjectData ??
                                                   throw new InvalidOperationException("Uninitialized property: " +
                                                       nameof(DatabaseProjectData));

    public ProjectForCreate DbMigrationProjectData => _dbMigrationProjectData ??
                                                      throw new InvalidOperationException("Uninitialized property: " +
                                                          nameof(DbMigrationProjectData));

    public static ConsoleAppCreatorData Create(AppCreatorBaseData appCreatorBaseData, string projectName,
        TemplateModel template)
    {
        var projectFolders = new List<string> { "Properties" };
        if (!template.UseDatabase)
            projectFolders.Add("Models");
        if (template.UseMenu)
            projectFolders.Add("MenuCommands");

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, projectName,
            projectName, EDotnetProjectType.Console, "", "Program", [.. projectFolders]);

        if (!template.UseDatabase)
            return new ConsoleAppCreatorData(appCreatorBaseData, mainProjectData, null, null, null, null,
                template.UseDatabase, template.UseMenu);

        var libProjectRepositoriesProjectData =
            ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
                $"Lib{projectName}Repositories",
                Array.Empty<string>());
        var doProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"Do{projectName}", new[] { "Models" });
        var databaseProjectData = ProjectForCreate.CreateClassLibProject(
            appCreatorBaseData.SolutionPath,
            $"{projectName}Db", new[] { "Models" });
        var dbMigrationProjectData =
            ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath, $"{projectName}DbMigration",
                new[] { "Migrations" });

        return new ConsoleAppCreatorData(appCreatorBaseData, mainProjectData, libProjectRepositoriesProjectData,
            doProjectData, databaseProjectData, dbMigrationProjectData, template.UseDatabase, template.UseMenu);
    }
}