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

    public static ConsoleAppCreatorData Create(AppCreatorBaseData appCreatorBaseData,
        AppProjectCreatorData par, TemplateModel template)
    {
        var projectFolders = new List<string>
        {
            "Properties",
            "Models"
        };
        if (template.UseMenu)
            projectFolders.Add("MenuCommands");

        //მთავარი პროექტი
        var mainProjectData = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, par.ProjectName,
            par.ProjectName, EDotnetProjectType.Console, "", "Program", projectFolders.ToArray());

        if (!template.UseDatabase)
            return new ConsoleAppCreatorData(appCreatorBaseData, mainProjectData, null, null, null, null,
                template.UseDatabase, template.UseMenu);

        var libProjectRepositoriesProjectData =
            ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
                $"Lib{par.ProjectName}Repositories",
                Array.Empty<string>());
        var doProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            $"Do{par.ProjectName}", new[] { "Models" });
        var databaseProjectData = ProjectForCreate.CreateClassLibProject(
            appCreatorBaseData.SolutionPath,
            $"{par.ProjectName}Db", new[] { "Models" });
        var dbMigrationProjectData =
            ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath, $"{par.ProjectName}DbMigration",
                new[] { "Migrations" });

        return new ConsoleAppCreatorData(appCreatorBaseData, mainProjectData, libProjectRepositoriesProjectData,
            doProjectData, databaseProjectData, dbMigrationProjectData, template.UseDatabase, template.UseMenu);
    }
}