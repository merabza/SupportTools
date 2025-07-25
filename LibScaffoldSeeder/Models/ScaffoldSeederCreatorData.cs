﻿using System.Collections.Generic;
using System.IO;
using LibAppProjectCreator;
using LibAppProjectCreator.Models;
using LibDotnetWork;

namespace LibScaffoldSeeder.Models;

public sealed class ScaffoldSeederCreatorData
{
    private ScaffoldSeederCreatorData(ProjectForCreate databaseScaffoldClassLibProject,
        ProjectForCreate dataSeedingClassLibProject, ProjectForCreate createProjectSeederCodeProject,
        ProjectForCreate getJsonFromProjectDbProject, ProjectForCreate dbMigrationProject,
        ProjectForCreate seedDbProject, ProjectForCreate fakeHostWebApiProject, AppCreatorBaseData appCreatorBaseData)
    {
        DatabaseScaffoldClassLibProject = databaseScaffoldClassLibProject;
        DataSeedingClassLibProject = dataSeedingClassLibProject;
        CreateProjectSeederCodeProject = createProjectSeederCodeProject;
        GetJsonFromProjectDbProject = getJsonFromProjectDbProject;
        DbMigrationProject = dbMigrationProject;
        SeedDbProject = seedDbProject;
        FakeHostWebApiProject = fakeHostWebApiProject;
        AppCreatorBaseData = appCreatorBaseData;
    }

    public AppCreatorBaseData AppCreatorBaseData { get; }
    public ProjectForCreate DatabaseScaffoldClassLibProject { get; }
    public ProjectForCreate DataSeedingClassLibProject { get; }
    public ProjectForCreate CreateProjectSeederCodeProject { get; }
    public ProjectForCreate GetJsonFromProjectDbProject { get; }
    public ProjectForCreate DbMigrationProject { get; }
    public ProjectForCreate SeedDbProject { get; }
    public ProjectForCreate FakeHostWebApiProject { get; }

    public static ScaffoldSeederCreatorData Create(AppCreatorBaseData appCreatorBaseData, string projectName,
        ScaffoldSeederCreatorParameters scaffoldSeederCreatorParameters)
    {
        //სკაფოლდინგის ბიბლიოთეკა
        var databaseScaffoldClassLibProjectName = $"{projectName}DbSc";
        var databaseScaffoldClassLibProject = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            databaseScaffoldClassLibProjectName, []);

        //ბაზაში ინფორმაციის ჩამყრელი ბიბლიოთეკა
        var dataSeedingClassLibProject = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            NamingStats.DataSeedingClassLibProjectName(scaffoldSeederCreatorParameters.ScaffoldSeederProjectName),
            ["CarcassSeeders", "ProjectSeeders", "Models", "Json"]);

        var createProjectSeederCodeProjectName =
            NamingStats.CreateProjectSeederCodeProjectName(scaffoldSeederCreatorParameters.ScaffoldSeederProjectName);
        //სიდერის კოდის შემქმნელი აპლიკაცია
        var createProjectSeederCodeProject = ProjectForCreate.Create(appCreatorBaseData.SolutionPath,
            createProjectSeederCodeProjectName, createProjectSeederCodeProjectName, EDotnetProjectType.Console,
            string.Empty, "Program", ["Models", "Properties"]);

        var getJsonFromScaffoldDbProjectName =
            NamingStats.GetJsonFromScaffoldDbProjectName(scaffoldSeederCreatorParameters.ScaffoldSeederProjectName);
        //ბაზიდან ცხრილების შიგთავსის json-ის სახით წამოღებისათვის საჭირო პროექტი
        var getJsonFromProjectDbProject = ProjectForCreate.Create(appCreatorBaseData.SolutionPath,
            getJsonFromScaffoldDbProjectName, getJsonFromScaffoldDbProjectName, EDotnetProjectType.Console,
            string.Empty, "Program", ["Models"]);

        var projectFolders = new List<string> { "Migrations" };
        var migrationSqlFilesFolder = scaffoldSeederCreatorParameters.MigrationSqlFilesFolder;
        //თუ მიგრაციის sql ფაილების ფოლდერი მითითებულია პარამეტრებში, ეს ფოლდერი არსებობს და შეიცავს ერთს მაინც *.sql ფაილს, მაშინ მიგრაციის პროექტში დაემატოს Sql ფოლდერი
        if (!string.IsNullOrWhiteSpace(migrationSqlFilesFolder) && Directory.Exists(migrationSqlFilesFolder))
        {
            var sqlDir = new DirectoryInfo(migrationSqlFilesFolder);
            if (sqlDir.GetFiles("*.sql").Length > 0)
                projectFolders.Add("Sql");
        }

        var dbMigrationProjectName =
            NamingStats.DbMigrationProjectName(scaffoldSeederCreatorParameters.ScaffoldSeederProjectName);
        //მიგრაციის პროექტი ბიბლიოთეკა
        var dbMigrationProject = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
            dbMigrationProjectName, [.. projectFolders]);

        var seedDbProjectName =
            NamingStats.SeedDbProjectName(scaffoldSeederCreatorParameters.ScaffoldSeederProjectName);
        //ინფორმაციის ბაზაში ჩაყრის პროცესის გამშვები პროექტი
        var seedDbProject = ProjectForCreate.Create(appCreatorBaseData.SolutionPath, seedDbProjectName,
            seedDbProjectName, EDotnetProjectType.Console, string.Empty, "Program", []);

        //პროექტი, რომელიც იქმნება მხოლოდ იმისათვის, რომ შესაძლებელი გახდეს dotnet EF ბრძანებების შესრულება შეცდომების გარეშე
        //მთავარი ამ პროექტში არის IHost-ის რეალიზაცია
        var fakeHostWebApiProject = ProjectForCreate.Create(appCreatorBaseData.SolutionPath,
            scaffoldSeederCreatorParameters.FakeHostProjectName, scaffoldSeederCreatorParameters.FakeHostProjectName,
            EDotnetProjectType.Web, "--no-https", "Program", []);

        return new ScaffoldSeederCreatorData(databaseScaffoldClassLibProject, dataSeedingClassLibProject,
            createProjectSeederCodeProject, getJsonFromProjectDbProject, dbMigrationProject, seedDbProject,
            fakeHostWebApiProject, appCreatorBaseData);

        //ProjectForCreate libProjectRepositoriesProjectData =
        //    ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath, $"Lib{par.ProjectName}Repositories",
        //        Array.Empty<string>());

        //ProjectForCreate doProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
        //    $"Do{par.ProjectName}", new[] { "Models" });
        //ProjectForCreate databaseProjectData = ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath,
        //    $"{par.ProjectName}Db", new[] { "Models" });
        //ProjectForCreate dbMigrationProjectData =
        //    ProjectForCreate.CreateClassLibProject(appCreatorBaseData.SolutionPath, $"{par.ProjectName}DbMigration",
        //        new[] { "Migrations" });
    }
}