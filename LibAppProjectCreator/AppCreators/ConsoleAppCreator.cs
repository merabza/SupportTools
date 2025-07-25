﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator.CodeCreators;
using LibAppProjectCreator.CodeCreators.Database;
using LibAppProjectCreator.JsonCreators;
using LibAppProjectCreator.Models;
using LibGitData.Models;
using LibGitWork;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.AppCreators;

public sealed class ConsoleAppCreator : AppCreatorBase
{
    private readonly ConsoleAppCreatorData _consoleAppCreatorData;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ConsoleAppCreator(ILogger logger, IHttpClientFactory httpClientFactory, string projectName, int indentSize,
        GitProjects gitProjects, GitRepos gitRepos, ConsoleAppCreatorData consoleAppCreatorData) : base(logger,
        httpClientFactory, projectName, indentSize, gitProjects, gitRepos,
        consoleAppCreatorData.AppCreatorBaseData.WorkPath, consoleAppCreatorData.AppCreatorBaseData.SecurityPath,
        consoleAppCreatorData.AppCreatorBaseData.SolutionPath)
    {
        _consoleAppCreatorData = consoleAppCreatorData;
    }

    protected override void PrepareProjectsData()
    {
        AddProject(_consoleAppCreatorData.MainProjectData);
        if (!_consoleAppCreatorData.UseDatabase)
            return;
        AddProject(_consoleAppCreatorData.DoProjectData);
        AddProject(_consoleAppCreatorData.LibProjectRepositoriesProjectData);
        AddProject(_consoleAppCreatorData.DatabaseProjectData);
        AddProject(_consoleAppCreatorData.DbMigrationProjectData);
    }

    protected override bool PrepareSpecific()
    {
        //რეფერენსების სიის შედგენა
        AddReference(_consoleAppCreatorData.MainProjectData, GitProjects.CliParameters);
        //AddReference(_consoleAppWithDatabaseCreatorData.MainProjectData, GitProjects.CliToolsData);
        AddReference(_consoleAppCreatorData.MainProjectData, GitProjects.CliTools);

        //პაკეტების სიის შედგენა
        AddPackage(_consoleAppCreatorData.MainProjectData, NuGetPackages.MicrosoftExtensionsLoggingAbstractions);

        if (!_consoleAppCreatorData.UseDatabase)
            return true;
        //რეფერენსების სიის შედგენა მთავარი პროექტისათვის
        AddReference(_consoleAppCreatorData.MainProjectData, GitProjects.CliParametersDataEdit);
        AddReference(_consoleAppCreatorData.MainProjectData, _consoleAppCreatorData.DatabaseProjectData);
        AddReference(_consoleAppCreatorData.MainProjectData, _consoleAppCreatorData.DoProjectData);
        AddReference(_consoleAppCreatorData.MainProjectData, _consoleAppCreatorData.LibProjectRepositoriesProjectData);
        AddReference(_consoleAppCreatorData.MainProjectData, _consoleAppCreatorData.DbMigrationProjectData);

        //რეფერენსების სიის შედგენა Do პროექტისათვის
        AddReference(_consoleAppCreatorData.DoProjectData, GitProjects.CliParameters);
        AddReference(_consoleAppCreatorData.DoProjectData, GitProjects.CliParametersDataEdit);
        AddReference(_consoleAppCreatorData.DoProjectData, GitProjects.CliToolsData);
        AddReference(_consoleAppCreatorData.DoProjectData, _consoleAppCreatorData.DatabaseProjectData);
        AddReference(_consoleAppCreatorData.DoProjectData, _consoleAppCreatorData.LibProjectRepositoriesProjectData);
        AddReference(_consoleAppCreatorData.DoProjectData, GitProjects.DbTools);

        //რეფერენსების სიის შედგენა LibProjectRepositories პროექტისათვის
        AddReference(_consoleAppCreatorData.LibProjectRepositoriesProjectData,
            _consoleAppCreatorData.DatabaseProjectData);

        //რეფერენსების სიის შედგენა Db პროექტისათვის
        AddReference(_consoleAppCreatorData.DatabaseProjectData, GitProjects.CliParameters);
        AddReference(_consoleAppCreatorData.DatabaseProjectData, GitProjects.SystemToolsShared);

        //რეფერენსების სიის შედგენა DbMigration პროექტისათვის
        AddReference(_consoleAppCreatorData.DbMigrationProjectData, _consoleAppCreatorData.DatabaseProjectData);

        //პაკეტების სიის შედგენა მთავარი პროექტისათვის
        AddPackage(_consoleAppCreatorData.MainProjectData, NuGetPackages.MicrosoftEntityFrameworkCoreDesign);

        //პაკეტების სიის შედგენა DoProject პროექტისათვის
        AddPackage(_consoleAppCreatorData.DoProjectData, NuGetPackages.MicrosoftExtensionsLoggingAbstractions);

        //პაკეტების სიის შედგენა Db პროექტისათვის
        AddPackage(_consoleAppCreatorData.DatabaseProjectData, NuGetPackages.MicrosoftEntityFrameworkCore);
        AddPackage(_consoleAppCreatorData.DatabaseProjectData, NuGetPackages.MicrosoftEntityFrameworkCoreRelational);
        AddPackage(_consoleAppCreatorData.DatabaseProjectData, NuGetPackages.MicrosoftEntityFrameworkCoreSqlServer);

        return true;
    }

    //პროექტის ტიპზე დამოკიდებული დამატებით საჭირო ფაილების შექმნა
    protected override Task<bool> MakeAdditionalFiles(CancellationToken cancellationToken = default)
    {
        ////---===მთავარი პროექტის ფაილები===---

        //შეიქმნას Program.cs. პროგრამის გამშვები კლასი
        Console.WriteLine("Creating Program.cs...");
        var programClassCreator = new ConsoleProgramClassCreator(Logger,
            _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, null,
            _consoleAppCreatorData.UseDatabase, "Program.cs");
        programClassCreator.CreateFileStructure();

        var doProject = _consoleAppCreatorData.UseDatabase
            ? _consoleAppCreatorData.DoProjectData
            : _consoleAppCreatorData.MainProjectData;

        var modelsPath = doProject.FoldersForCreate["Models"];

        //თუ ბაზა გვჭირდება, ვიყენებთ do პროექტს
        var inNameSpace = doProject.ProjectName;

        //შეიქმნას პროექტის პარამეტრების კლასი
        Console.WriteLine($"Creating {ProjectName}Parameters.cs...");
        var projectParametersClassCreator = new ProjectParametersClassCreator(Logger, modelsPath, ProjectName,
            inNameSpace, _consoleAppCreatorData.UseDatabase, _consoleAppCreatorData.UseMenu,
            $"{ProjectName}Parameters.cs");
        projectParametersClassCreator.CreateFileStructure();

        if (_consoleAppCreatorData.UseDatabase)
            MakeFilesWhenUseDatabase();

        if (_consoleAppCreatorData.UseMenu)
            MakeFilesWhenUseMenu();
        else
            MakeFilesWhenNotUseMenu();

        //launchSettings.json ფაილის შექმნა (პროგრამის გაშვებისას პარამეტრების მითითებისათვის
        Console.WriteLine("Creating launchSettings.json...");
        var launchSettingsJsonCreator = new ConsoleAppLaunchSettingsJsonCreator(
            _consoleAppCreatorData.MainProjectData.FoldersForCreate["Properties"], ProjectName, SecurityPath);
        if (!launchSettingsJsonCreator.Create())
            return Task.FromResult(false);

        //პარამეტრების json ფაილის შექმნა
        var projectParametersJsonCreator = new ProjectParametersJsonCreator(SecurityPath, ProjectName);
        if (!projectParametersJsonCreator.Create())
            return Task.FromResult(false);

        //Console.WriteLine("Creating main project .gitignore...");
        //var mainProjectGitIgnoreCreator = new MainProjectGitIgnoreCreator(Logger,
        //    _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, ".gitignore");
        //mainProjectGitIgnoreCreator.CreateFileStructure();

        var gitProcessor = new GitProcessor(true, Logger, SolutionPath);
        return Task.FromResult(gitProcessor.Initialise().IsNone);
    }

    private void MakeFilesWhenNotUseMenu()
    {
        //შეიქმნას პროგრამის მთავარი მუშა კლასი
        Console.WriteLine($"Creating {ProjectName}.cs...");
        var projectMainClassCreator = new ProjectMainClassCreator(Logger,
            _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _consoleAppCreatorData.UseDatabase,
            $"{ProjectName}.cs");
        projectMainClassCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseMenu()
    {
        //var mainProjectModelsPath =
        //    _consoleAppCreatorData.MainProjectData.FoldersForCreate["Models"];
        var menuCommands = _consoleAppCreatorData.MainProjectData.FoldersForCreate["MenuCommands"];

        //შეიქმნას პროექტის პარამეტრების რედაქტირების კლასი
        Console.WriteLine($"Creating {ProjectName}ParametersEditor.cs...");
        var projectParametersEditorClassCreator = new ProjectParametersEditorClassCreator(Logger,
            _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _consoleAppCreatorData.UseDatabase,
            $"{ProjectName}ParametersEditor.cs");
        projectParametersEditorClassCreator.CreateFileStructure();

        //შეიქმნას პროგრამის მთავარი მუშა კლასი
        Console.WriteLine($"Creating {ProjectName}.cs...");
        var projectWithMenuMainClassCreator = new ProjectMainClassCreatorForCliAppWithMenu(Logger,
            _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _consoleAppCreatorData.UseDatabase,
            $"{ProjectName}.cs");
        projectWithMenuMainClassCreator.CreateFileStructure();

        //შეიქმნას ამოცანის წაშლის ბრძანების კლასი
        Console.WriteLine("Creating DeleteTaskCommand.cs...");
        var deleteTaskCommandCreator = new DeleteTaskCommandCreator(Logger, menuCommands, ProjectName,
            _consoleAppCreatorData.UseDatabase, "DeleteTaskCommand.cs");
        deleteTaskCommandCreator.CreateFileStructure();

        //შეიქმნას ამოცანის მოდელის კლასი
        Console.WriteLine("Creating TaskModel.cs...");
        var taskModelCreator = new TaskModelCreator(Logger,
            _consoleAppCreatorData.UseDatabase
                ? _consoleAppCreatorData.DoProjectData.FoldersForCreate["Models"]
                : _consoleAppCreatorData.MainProjectData.FoldersForCreate["Models"], ProjectName,
            _consoleAppCreatorData.UseDatabase, "TaskModel.cs");
        taskModelCreator.CreateFileStructure();

        //შეიქმნას ამოცანის რედაქტირების ბრძანების კლასი
        Console.WriteLine("Creating EditTaskNameCommand.cs...");
        var editTaskNameCommandCreator = new EditTaskNameCommandCreator(Logger, menuCommands, ProjectName,
            _consoleAppCreatorData.UseDatabase, "EditTaskNameCommand.cs");
        editTaskNameCommandCreator.CreateFileStructure();

        //შეიქმნას ახალი ამოცანის შექმნის ბრძანების კლასი
        Console.WriteLine("Creating NewTaskCommand.cs...");
        var newTaskCommandCreator = new NewTaskCommandCreator(Logger, menuCommands, ProjectName,
            _consoleAppCreatorData.UseDatabase, "NewTaskCommand.cs");
        newTaskCommandCreator.CreateFileStructure();

        //შეიქმნას ახალი ამოცანის შექმნის ბრძანების კლასი
        Console.WriteLine("Creating TaskCommand.cs...");
        var taskCommandCreator = new TaskCommandCreator(Logger, menuCommands, ProjectName,
            _consoleAppCreatorData.UseDatabase, "TaskCommand.cs");
        taskCommandCreator.CreateFileStructure();

        //შეიქმნას ახალი ამოცანის შექმნის ბრძანების კლასი
        Console.WriteLine("Creating TaskSubMenuCommand.cs...");
        var taskSubMenuCommandCreator = new TaskSubMenuCommandCreator(Logger, menuCommands, ProjectName,
            _consoleAppCreatorData.UseDatabase, "TaskSubMenuCommand.cs");
        taskSubMenuCommandCreator.CreateFileStructure();

        //შეიქმნას ამოცანის გამშვები კლასი
        Console.WriteLine($"Creating {ProjectName}TaskRunner.cs...");
        var projectTaskRunnerCreator = new ProjectTaskRunnerCreator(Logger,
            _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _consoleAppCreatorData.UseDatabase,
            $"{ProjectName}TaskRunner.cs");
        projectTaskRunnerCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseDatabase()
    {
        //შეიქმნას აპლიკაციის სერვისების შემქმნელი კლასი StatProgramAttr.cs
        Console.WriteLine($"Creating {ProjectName}ServicesCreator.cs...");
        var projectServicesCreatorClassCreator = new ProjectServicesCreatorClassCreator(Logger,
            _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, $"{ProjectName}ServicesCreator.cs");
        projectServicesCreatorClassCreator.CreateFileStructure();

        Console.WriteLine($"Creating {ProjectName}DesignTimeDbContextFactory.cs...");
        var projectDesignTimeDbContextFactoryClassCreator = new ProjectDesignTimeDbContextFactoryClassCreator(Logger,
            _consoleAppCreatorData.MainProjectData.ProjectFullPath, ProjectName,
            $"{ProjectName}DesignTimeDbContextFactory.cs");
        projectDesignTimeDbContextFactoryClassCreator.CreateFileStructure();

        //---===libProjectRepositories პროექტის ფაილები===---
        //შეიქმნას ბაზის რეპოზიტორიის შემქმნელი ინტერფეისი
        Console.WriteLine($"Creating I{ProjectName}RepositoryCreatorFactory.cs...");
        var repositoryCreatorFactoryInterfaceCreator = new RepositoryCreatorFactoryInterfaceCreator(Logger,
            _consoleAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"I{ProjectName}RepositoryCreatorFactory.cs");
        repositoryCreatorFactoryInterfaceCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორიის შემქმნელი
        Console.WriteLine($"Creating {ProjectName}RepositoryCreatorFactory.cs...");
        var repositoryCreatorFactoryCreator = new RepositoryCreatorFactoryCreator(Logger,
            _consoleAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"{ProjectName}RepositoryCreatorFactory.cs");
        repositoryCreatorFactoryCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორია
        Console.WriteLine($"Creating I{ProjectName}Repository.cs...");
        var repositoryInterfaceCreator = new RepositoryInterfaceCreator(Logger,
            _consoleAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"I{ProjectName}Repository.cs");
        repositoryInterfaceCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორია
        Console.WriteLine($"Creating {ProjectName}Repository.cs...");
        var repositoryClassCreator = new RepositoryClassCreator(Logger,
            _consoleAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"{ProjectName}Repository.cs");
        repositoryClassCreator.CreateFileStructure();

        //---===ბაზის კონტექსტის პროექტის ფაილები===---
        Console.WriteLine($"Creating {ProjectName}DbContext.cs...");
        var dbContextClassCreator = new DbContextClassCreator(Logger,
            _consoleAppCreatorData.DatabaseProjectData.ProjectFullPath, ProjectName, $"{ProjectName}DbContext.cs");
        dbContextClassCreator.CreateFileStructure();

        Console.WriteLine("Creating DesignTimeDbContextFactory.cs...");
        var designTimeDbContextFactoryClassCreator = new DesignTimeDbContextFactoryClassCreator(Logger,
            _consoleAppCreatorData.DatabaseProjectData.ProjectFullPath, ProjectName, "DesignTimeDbContextFactory.cs");
        designTimeDbContextFactoryClassCreator.CreateFileStructure();

        Console.WriteLine("Creating TestModel.cs...");
        var testModelClassCreator = new TestModelClassCreator(Logger,
            _consoleAppCreatorData.DatabaseProjectData.FoldersForCreate["Models"], ProjectName, false, false,
            "TestModel.cs");
        testModelClassCreator.CreateFileStructure();
    }
}