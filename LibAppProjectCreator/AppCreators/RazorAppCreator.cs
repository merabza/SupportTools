using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator.CodeCreators;
using LibAppProjectCreator.JsonCreators;
using LibAppProjectCreator.Models;
using LibGitData.Models;
using LibGitWork;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.AppCreators;

public sealed class RazorAppCreator : AppCreatorBase
{
    private readonly RazorAppCreatorData _razorAppCreatorData;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RazorAppCreator(ILogger logger, IHttpClientFactory httpClientFactory, string projectName, int indentSize,
        GitProjects gitProjects, GitRepos gitRepos, RazorAppCreatorData razorAppCreatorData) : base(logger,
        httpClientFactory, projectName, indentSize, gitProjects, gitRepos,
        razorAppCreatorData.AppCreatorBaseData.WorkPath, razorAppCreatorData.AppCreatorBaseData.SecurityPath,
        razorAppCreatorData.AppCreatorBaseData.SolutionPath)
    {
        _razorAppCreatorData = razorAppCreatorData;
    }

    protected override void PrepareProjectsData()
    {
        AddProject(_razorAppCreatorData.MainProjectData);
    }

    protected override bool PrepareSpecific()
    {
        //რეფერენსების სიის შედგენა
        AddReference(_razorAppCreatorData.MainProjectData, GitProjects.CliParameters);
        //AddReference(_razorAppWithDatabaseCreatorData.MainProjectData, GitProjects.CliToolsData);
        AddReference(_razorAppCreatorData.MainProjectData, GitProjects.CliTools);

        //პაკეტების სიის შედგენა
        AddPackage(_razorAppCreatorData.MainProjectData, NuGetPackages.MicrosoftExtensionsLoggingAbstractions);

        //რეფერენსების სიის შედგენა მთავარი პროექტისათვის
        AddReference(_razorAppCreatorData.MainProjectData, GitProjects.CliParametersDataEdit);

        //პაკეტების სიის შედგენა მთავარი პროექტისათვის
        AddPackage(_razorAppCreatorData.MainProjectData, NuGetPackages.MicrosoftEntityFrameworkCoreDesign);

        return true;
    }

    //პროექტის ტიპზე დამოკიდებული დამატებით საჭირო ფაილების შექმნა
    protected override Task<bool> MakeAdditionalFiles(CancellationToken cancellationToken = default)
    {
        ////---===მთავარი პროექტის ფაილები===---

        //შეიქმნას Program.cs. პროგრამის გამშვები კლასი
        Console.WriteLine("Creating Program.cs...");
        //var programClassCreator = new RazorProgramClassCreator(Logger,
        //    _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, null, _razorAppCreatorData.UseDatabase,
        //    _razorAppCreatorData.UseMenu, "Program.cs");
        //programClassCreator.CreateFileStructure();

        //var doProject = _razorAppCreatorData.UseDatabase
        //    ? _razorAppCreatorData.DoProjectData
        //    : _razorAppCreatorData.MainProjectData;

        //var modelsPath = doProject.FoldersForCreate["Models"];

        ////თუ ბაზა გვჭირდება, ვიყენებთ do პროექტს
        //var inNameSpace = doProject.ProjectName;

        ////შეიქმნას პროექტის პარამეტრების კლასი
        //Console.WriteLine($"Creating {ProjectName}Parameters.cs...");
        //var projectParametersClassCreator = new ProjectParametersClassCreator(Logger, modelsPath, ProjectName,
        //    inNameSpace, _razorAppCreatorData.UseDatabase, _razorAppCreatorData.UseMenu, $"{ProjectName}Parameters.cs");
        //projectParametersClassCreator.CreateFileStructure();

        //if (_razorAppCreatorData.UseDatabase)
        //    MakeFilesWhenUseDatabase();

        //if (_razorAppCreatorData.UseMenu)
        //    MakeFilesWhenUseMenu();
        //else
        //    MakeFilesWhenNotUseMenu();

        ////launchSettings.json ფაილის შექმნა (პროგრამის გაშვებისას პარამეტრების მითითებისათვის
        //Console.WriteLine("Creating launchSettings.json...");
        //var launchSettingsJsonCreator = new RazorAppLaunchSettingsJsonCreator(
        //    _razorAppCreatorData.MainProjectData.FoldersForCreate["Properties"], ProjectName, SecurityPath);
        //if (!launchSettingsJsonCreator.Create())
        //    return Task.FromResult(false);

        //პარამეტრების json ფაილის შექმნა
        var projectParametersJsonCreator = new ProjectParametersJsonCreator(SecurityPath, ProjectName);
        if (!projectParametersJsonCreator.Create())
            return Task.FromResult(false);

        //Console.WriteLine("Creating main project .gitignore...");
        //var mainProjectGitIgnoreCreator = new MainProjectGitIgnoreCreator(Logger,
        //    _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, ".gitignore");
        //mainProjectGitIgnoreCreator.CreateFileStructure();

        var gitProcessor = new GitProcessor(true, Logger, SolutionPath);
        return Task.FromResult(gitProcessor.Initialise().IsNone);
    }

    //private void MakeFilesWhenNotUseMenu()
    //{
    //    //შეიქმნას პროგრამის მთავარი მუშა კლასი
    //    Console.WriteLine($"Creating {ProjectName}.cs...");
    //    var projectMainClassCreator = new ProjectMainClassCreator(Logger,
    //        _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _razorAppCreatorData.UseDatabase,
    //        $"{ProjectName}.cs");
    //    projectMainClassCreator.CreateFileStructure();
    //}

    //private void MakeFilesWhenUseMenu()
    //{
    //    //var mainProjectModelsPath =
    //    //    _razorAppCreatorData.MainProjectData.FoldersForCreate["Models"];
    //    var menuCommands = _razorAppCreatorData.MainProjectData.FoldersForCreate["MenuCommands"];

    //    //შეიქმნას პროექტის პარამეტრების რედაქტირების კლასი
    //    Console.WriteLine($"Creating {ProjectName}ParametersEditor.cs...");
    //    var projectParametersEditorClassCreator = new ProjectParametersEditorClassCreator(Logger,
    //        _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _razorAppCreatorData.UseDatabase,
    //        $"{ProjectName}ParametersEditor.cs");
    //    projectParametersEditorClassCreator.CreateFileStructure();

    //    //შეიქმნას პროგრამის მთავარი მუშა კლასი
    //    Console.WriteLine($"Creating {ProjectName}.cs...");
    //    var projectWithMenuMainClassCreator = new ProjectMainClassCreatorForCliAppWithMenu(Logger,
    //        _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _razorAppCreatorData.UseDatabase,
    //        $"{ProjectName}.cs");
    //    projectWithMenuMainClassCreator.CreateFileStructure();

    //    //შეიქმნას ამოცანის წაშლის ბრძანების კლასი
    //    Console.WriteLine("Creating DeleteTaskCommand.cs...");
    //    var deleteTaskCommandCreator = new DeleteTaskCommandCreator(Logger, menuCommands, ProjectName,
    //        _razorAppCreatorData.UseDatabase, "DeleteTaskCommand.cs");
    //    deleteTaskCommandCreator.CreateFileStructure();

    //    //შეიქმნას ამოცანის მოდელის კლასი
    //    Console.WriteLine("Creating TaskModel.cs...");
    //    var taskModelCreator = new TaskModelCreator(Logger,
    //        _razorAppCreatorData.UseDatabase
    //            ? _razorAppCreatorData.DoProjectData.FoldersForCreate["Models"]
    //            : _razorAppCreatorData.MainProjectData.FoldersForCreate["Models"], ProjectName,
    //        _razorAppCreatorData.UseDatabase, "TaskModel.cs");
    //    taskModelCreator.CreateFileStructure();

    //    //შეიქმნას ამოცანის რედაქტირების ბრძანების კლასი
    //    Console.WriteLine("Creating EditTaskNameCommand.cs...");
    //    var editTaskNameCommandCreator = new EditTaskNameCommandCreator(Logger, menuCommands, ProjectName,
    //        _razorAppCreatorData.UseDatabase, "EditTaskNameCommand.cs");
    //    editTaskNameCommandCreator.CreateFileStructure();

    //    //შეიქმნას ახალი ამოცანის შექმნის ბრძანების კლასი
    //    Console.WriteLine("Creating NewTaskCommand.cs...");
    //    var newTaskCommandCreator = new NewTaskCommandCreator(Logger, menuCommands, ProjectName,
    //        _razorAppCreatorData.UseDatabase, "NewTaskCommand.cs");
    //    newTaskCommandCreator.CreateFileStructure();

    //    //შეიქმნას ახალი ამოცანის შექმნის ბრძანების კლასი
    //    Console.WriteLine("Creating TaskCommand.cs...");
    //    var taskCommandCreator = new TaskCommandCreator(Logger, menuCommands, ProjectName,
    //        _razorAppCreatorData.UseDatabase, "TaskCommand.cs");
    //    taskCommandCreator.CreateFileStructure();

    //    //შეიქმნას ახალი ამოცანის შექმნის ბრძანების კლასი
    //    Console.WriteLine("Creating TaskSubMenuCommand.cs...");
    //    var taskSubMenuCommandCreator = new TaskSubMenuCommandCreator(Logger, menuCommands, ProjectName,
    //        _razorAppCreatorData.UseDatabase, "TaskSubMenuCommand.cs");
    //    taskSubMenuCommandCreator.CreateFileStructure();

    //    //შეიქმნას ამოცანის გამშვები კლასი
    //    Console.WriteLine($"Creating {ProjectName}TaskRunner.cs...");
    //    var projectTaskRunnerCreator = new ProjectTaskRunnerCreator(Logger,
    //        _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, _razorAppCreatorData.UseDatabase,
    //        $"{ProjectName}TaskRunner.cs");
    //    projectTaskRunnerCreator.CreateFileStructure();
    //}

    //private void MakeFilesWhenUseDatabase()
    //{
    //    //შეიქმნას აპლიკაციის სერვისების შემქმნელი კლასი StatProgramAttr.cs
    //    Console.WriteLine($"Creating {ProjectName}ServicesCreator.cs...");
    //    var projectServicesCreatorClassCreator = new ProjectServicesCreatorClassCreator(Logger,
    //        _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName, $"{ProjectName}ServicesCreator.cs");
    //    projectServicesCreatorClassCreator.CreateFileStructure();

    //    Console.WriteLine($"Creating {ProjectName}DesignTimeDbContextFactory.cs...");
    //    var projectDesignTimeDbContextFactoryClassCreator = new ProjectDesignTimeDbContextFactoryClassCreator(Logger,
    //        _razorAppCreatorData.MainProjectData.ProjectFullPath, ProjectName,
    //        $"{ProjectName}DesignTimeDbContextFactory.cs");
    //    projectDesignTimeDbContextFactoryClassCreator.CreateFileStructure();

    //    //---===libProjectRepositories პროექტის ფაილები===---
    //    //შეიქმნას ბაზის რეპოზიტორიის შემქმნელი ინტერფეისი
    //    Console.WriteLine($"Creating I{ProjectName}RepositoryCreatorFactory.cs...");
    //    var repositoryCreatorFactoryInterfaceCreator = new RepositoryCreatorFactoryInterfaceCreator(Logger,
    //        _razorAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
    //        $"I{ProjectName}RepositoryCreatorFactory.cs");
    //    repositoryCreatorFactoryInterfaceCreator.CreateFileStructure();

    //    //შეიქმნას ბაზის რეპოზიტორიის შემქმნელი
    //    Console.WriteLine($"Creating {ProjectName}RepositoryCreatorFactory.cs...");
    //    var repositoryCreatorFactoryCreator = new RepositoryCreatorFactoryCreator(Logger,
    //        _razorAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
    //        $"{ProjectName}RepositoryCreatorFactory.cs");
    //    repositoryCreatorFactoryCreator.CreateFileStructure();

    //    //შეიქმნას ბაზის რეპოზიტორია
    //    Console.WriteLine($"Creating I{ProjectName}Repository.cs...");
    //    var repositoryInterfaceCreator = new RepositoryInterfaceCreator(Logger,
    //        _razorAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
    //        $"I{ProjectName}Repository.cs");
    //    repositoryInterfaceCreator.CreateFileStructure();

    //    //შეიქმნას ბაზის რეპოზიტორია
    //    Console.WriteLine($"Creating {ProjectName}Repository.cs...");
    //    var repositoryClassCreator = new RepositoryClassCreator(Logger,
    //        _razorAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
    //        $"{ProjectName}Repository.cs");
    //    repositoryClassCreator.CreateFileStructure();

    //    //---===ბაზის კონტექსტის პროექტის ფაილები===---
    //    Console.WriteLine($"Creating {ProjectName}DbContext.cs...");
    //    var dbContextClassCreator = new DbContextClassCreator(Logger,
    //        _razorAppCreatorData.DatabaseProjectData.ProjectFullPath, ProjectName, $"{ProjectName}DbContext.cs");
    //    dbContextClassCreator.CreateFileStructure();

    //    Console.WriteLine("Creating DesignTimeDbContextFactory.cs...");
    //    var designTimeDbContextFactoryClassCreator = new DesignTimeDbContextFactoryClassCreator(Logger,
    //        _razorAppCreatorData.DatabaseProjectData.ProjectFullPath, ProjectName, "DesignTimeDbContextFactory.cs");
    //    designTimeDbContextFactoryClassCreator.CreateFileStructure();

    //    Console.WriteLine("Creating TestModel.cs...");
    //    var testModelClassCreator = new TestModelClassCreator(Logger,
    //        _razorAppCreatorData.DatabaseProjectData.FoldersForCreate["Models"], ProjectName, false, false,
    //        "TestModel.cs");
    //    testModelClassCreator.CreateFileStructure();
    //}
}