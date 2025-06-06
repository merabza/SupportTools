using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CodeTools;
using LibAppProjectCreator.CodeCreators;
using LibAppProjectCreator.CodeCreators.CarcassAndDatabase;
using LibAppProjectCreator.CodeCreators.Database;
using LibAppProjectCreator.CodeCreators.Installers;
using LibAppProjectCreator.JsonCreators;
using LibAppProjectCreator.Models;
using LibGitData.Models;
using LibGitWork;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemToolsShared;

namespace LibAppProjectCreator.AppCreators;

public sealed class ApiAppCreator : AppCreatorBase
{
    private readonly ApiAppCreatorData _apiAppCreatorData;
    private readonly string? _projectShortName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiAppCreator(ILogger logger, IHttpClientFactory httpClientFactory, string? projectShortName,
        string projectName, int indentSize, GitProjects gitProjects, GitRepos gitRepos,
        ApiAppCreatorData apiAppCreatorData) : base(logger, httpClientFactory, projectName, indentSize, gitProjects,
        gitRepos, apiAppCreatorData.AppCreatorBaseData.WorkPath, apiAppCreatorData.AppCreatorBaseData.SecurityPath,
        apiAppCreatorData.AppCreatorBaseData.SolutionPath)
    {
        _projectShortName = projectShortName;
        _apiAppCreatorData = apiAppCreatorData;
    }

    //protected override void PrepareFoldersForCheckAndClear()
    //{
    //    base.PrepareFoldersForCheckAndClear();
    //    if (!string.IsNullOrWhiteSpace(_apiAppCreatorData.DbPartPath))
    //        FoldersForCheckAndClear.Add(_apiAppCreatorData.DbPartPath);
    //}

    //პროექტის ტიპისათვის დამახასიათებელი დამატებითი პარამეტრების გამოანგარიშება
    protected override bool PrepareSpecific()
    {
        if (_apiAppCreatorData is { UseIdentity: true, UseCarcass: true })
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.CarcassIdentity);

        if (_apiAppCreatorData.UseReact)
            AddPackage(_apiAppCreatorData.MainProjectData, NuGetPackages.MicrosoftAspNetCoreSpaServicesExtensions);

        if (_apiAppCreatorData.UseSignalR)
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.SignalRMessages);

        if (_apiAppCreatorData.UseFluentValidation)
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.FluentValidationInstaller);

        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.SystemToolsShared);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.ApiExceptionHandler);

        if (_apiAppCreatorData.UseReCounter)
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.ReCounterServiceInstaller);

        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.TestToolsApi);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.StaticFilesTools);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.WebInstallers);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.ConfigurationEncrypt);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.SerilogLogger);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.SwaggerTools);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.WindowsServiceTools);

        if (_apiAppCreatorData.UseCarcass)
        {
            AddReference(_apiAppCreatorData.DatabaseProjectData, GitProjects.CarcassDb);
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.CarcassIdentity);
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.CarcassRepositories);
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.BackendCarcassApi);
        }

        if (!_apiAppCreatorData.UseDatabase)
            return true;

        //რეფერენსების სიის შედგენა მთავარი პროექტისათვის
        AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.DatabaseProjectData);
        AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.LibProjectRepositoriesProjectData);

        if (!_apiAppCreatorData.UseDbPartFolderForDatabaseProjects)
            AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.DbMigrationProjectData);

        //რეფერენსების სიის შედგენა LibProjectRepositories პროექტისათვის
        AddReference(_apiAppCreatorData.LibProjectRepositoriesProjectData, _apiAppCreatorData.DatabaseProjectData);

        //რეფერენსების სიის შედგენა Db პროექტისათვის
        AddReference(_apiAppCreatorData.DatabaseProjectData, GitProjects.SystemToolsShared);
        AddReference(_apiAppCreatorData.DatabaseProjectData, GitProjects.WebInstallers);

        if (!_apiAppCreatorData.UseDbPartFolderForDatabaseProjects)
            //რეფერენსების სიის შედგენა DbMigration პროექტისათვის
            AddReference(_apiAppCreatorData.DbMigrationProjectData, _apiAppCreatorData.DatabaseProjectData);

        //პაკეტების სიის შედგენა მთავარი პროექტისათვის
        AddPackage(_apiAppCreatorData.MainProjectData, NuGetPackages.MicrosoftEntityFrameworkCoreDesign);

        //პაკეტების სიის შედგენა Db პროექტისათვის
        AddPackage(_apiAppCreatorData.DatabaseProjectData, NuGetPackages.MicrosoftEntityFrameworkCore);
        AddPackage(_apiAppCreatorData.DatabaseProjectData, NuGetPackages.MicrosoftEntityFrameworkCoreRelational);
        AddPackage(_apiAppCreatorData.DatabaseProjectData, NuGetPackages.MicrosoftEntityFrameworkCoreSqlServer);

        if (!_apiAppCreatorData.UseCarcass)
            return true;

        AddReference(_apiAppCreatorData.RepositoriesProjectData, _apiAppCreatorData.DatabaseProjectData);
        AddReference(_apiAppCreatorData.RepositoriesProjectData, GitProjects.CarcassRepositories);
        AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.RepositoriesProjectData);
        AddReference(_apiAppCreatorData.DatabaseProjectData, GitProjects.CarcassDb);

        if (!_apiAppCreatorData.UseReact)
            return true;

        //ფრონტის რეფერენსის დამატება ჯერ ვერ მოვახერხე. ეს არ მუშაობს და ამიტომ დავაკომენტარე
        //AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.FrontendProjectData);

        return true;
    }

    protected override async Task<bool> MakeAdditionalFiles(CancellationToken cancellationToken = default)
    {
        var appSettingsJsonJObject = new JObject();
        var userSecretJsonJObject = new JObject();
        var forEncodeAppSettingsJsonKeys = new List<string>();
        var keyPart1 = Guid.NewGuid().ToString("N");

        //შეიქმნას Program.cs. პროგრამის გამშვები კლასი
        Console.WriteLine("Creating Program.cs...");
        var programClassCreator = new ApiProgramClassCreator(Logger, _apiAppCreatorData.MainProjectData.ProjectFullPath,
            ProjectName, keyPart1, _apiAppCreatorData.UseDatabase, _apiAppCreatorData.UseCarcass,
            _apiAppCreatorData.UseIdentity, _apiAppCreatorData.UseReCounter, _apiAppCreatorData.UseSignalR,
            _apiAppCreatorData.UseFluentValidation, _apiAppCreatorData.UseReact, _apiAppCreatorData.DbPartProjectName,
            "Program.cs");
        programClassCreator.CreateFileStructure();

        var modelsPath = _apiAppCreatorData.MainProjectData.FoldersForCreate["Models"];

        if (_apiAppCreatorData is { UseIdentity: true, UseCarcass: false })
            MakeFilesWhenUseIdentityAndNotUseCarcass(modelsPath);

        if (_apiAppCreatorData.UseCarcass)
            MakeFilesWhenUseCarcass();

        if (_apiAppCreatorData.UseDatabase)
            MakeFilesWhenUseDatabase(appSettingsJsonJObject, userSecretJsonJObject, forEncodeAppSettingsJsonKeys);

        if (_apiAppCreatorData is { UseCarcass: true, UseDatabase: true })
            MakeFilesWhenUseCarcassAndUseDatabase();

        if (_apiAppCreatorData.UseIdentity)
            MakeFilesWhenUseIdentity(appSettingsJsonJObject, userSecretJsonJObject, forEncodeAppSettingsJsonKeys);

        Console.WriteLine("Creating KestrelOptions...");
        var kestrelOptionsCreator = new KestrelOptionsCreator(appSettingsJsonJObject);
        kestrelOptionsCreator.Run();

        if (_apiAppCreatorData.UseReact)
            if (!MakeFilesWhenUseReact())
                return false;

        Console.WriteLine("Creating LoggerProperties...");
        var loggerSettingsCreator = new LoggerSettingsCreator(ProjectName, appSettingsJsonJObject,
            userSecretJsonJObject, forEncodeAppSettingsJsonKeys);
        loggerSettingsCreator.Run();

        if (_apiAppCreatorData.UseReact)
        {
            Console.WriteLine("Creating Cors Properties...");
            var corsSettingsCreator = new CorsSettingsCreator(appSettingsJsonJObject, userSecretJsonJObject,
                forEncodeAppSettingsJsonKeys);
            corsSettingsCreator.Run();
        }

        Console.WriteLine("Create AppSettingsVersion...");
        appSettingsJsonJObject.Add(
            new JProperty("VersionInfo", new JObject(new JProperty("AppSettingsVersion", "1.1"))));

        Console.WriteLine("Creating SettingsFiles...");

        var settingsFilesCreator = new SettingsFilesCreator(Logger, _apiAppCreatorData.MainProjectData.ProjectFullPath,
            _apiAppCreatorData.MainProjectData.ProjectFileFullName, appSettingsJsonJObject,
            forEncodeAppSettingsJsonKeys, userSecretJsonJObject, keyPart1);

        if (!await settingsFilesCreator.Run(cancellationToken))
            return false;

        Console.WriteLine("Creating launchSettings.json...");
        var apiAppLaunchSettingsJsonCreator =
            new ApiAppLaunchSettingsJsonCreator(ProjectName, _apiAppCreatorData.MainProjectData.ProjectFullPath);

        if (!apiAppLaunchSettingsJsonCreator.Create())
            return false;

        const string cSharp = "CSharp";
        return CopyGitIgnoreFile(cSharp, _apiAppCreatorData.AppCreatorBaseData.SolutionPath);
    }

    private bool CopyGitIgnoreFile(string gitignoreFileKey, string folderForGitIgnore)
    {
        Console.WriteLine("Coping .gitignore file...");

        var gitIgnoreModelFilePaths = _apiAppCreatorData.AppCreatorBaseData.GitIgnoreModelFilePaths;
        const string gitignore = ".gitignore";

        if (!gitIgnoreModelFilePaths.ContainsKey(gitignoreFileKey))
        {
            Logger.LogError("gitIgnoreModelFilePaths are not contains {gitignoreFileKey} key", gitignoreFileKey);
            return false;
        }

        if (!File.Exists(gitIgnoreModelFilePaths[gitignoreFileKey]))
        {
            Logger.LogError("{gitIgnoreModelFilePaths[gitignoreFileKey]} file is not found",
                gitIgnoreModelFilePaths[gitignoreFileKey]);
            return false;
        }

        if (!Directory.Exists(folderForGitIgnore))
        {
            Logger.LogError("folder {folderForGitIgnore} is not found", folderForGitIgnore);
            return false;
        }

        File.Copy(gitIgnoreModelFilePaths[gitignoreFileKey], Path.Combine(folderForGitIgnore, gitignore));
        return true;
    }

    private bool MakeFilesWhenUseReact()
    {
        if (string.IsNullOrWhiteSpace(_apiAppCreatorData.FrontendProjectData.SolutionFolderName))
        {
            Logger.LogError("_apiAppCreatorData.FrontendProjectData.SolutionFolderName is empty");
            return false;
        }

        const string react = "React";
        return CopyGitIgnoreFile(react,
            Path.Combine(WorkPath, _apiAppCreatorData.FrontendProjectData.SolutionFolderName));

        //Console.WriteLine("Creating ReactInstaller.cs...");
        //var reactInstallerClassCreator = new ReactInstallerClassCreator(Logger, installersPath, Par.ProjectName,
        //    _apiAppCreatorData.UseBackgroundTasks, "ReactInstaller.cs");
        //reactInstallerClassCreator.CreateFileStructure();

        //var pagesPath = _apiAppCreatorData.MainProjectData.FoldersForCreate["Pages"];

        //Console.WriteLine("Creating _ViewImports.cshtml...");
        //var viewImportsPageCreator =
        //    new ViewImportsPageCreator(Logger, pagesPath, ProjectName, "_ViewImports.cshtml");
        //viewImportsPageCreator.CreateFileStructure();

        //Console.WriteLine("Creating Error.cshtml...");
        //var errorPageCreator = new ErrorPageCreator(Logger, pagesPath, "Error.cshtml");
        //errorPageCreator.CreateFileStructure();

        //Console.WriteLine("Creating Error.cshtml.cs...");
        //var errorModelClassCreator = new ErrorModelClassCreator(Logger, pagesPath, ProjectName, "Error.cshtml.cs");
        //errorModelClassCreator.CreateFileStructure();
    }

    private static void MakeFilesWhenUseIdentity(JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys)
    {
        Console.WriteLine("Creating Identity Settings...");
        var identitySettingsCreator = new IdentitySettingsCreator(appSettingsJsonJObject, userSecretJsonJObject,
            forEncodeAppSettingsJsonKeys);
        identitySettingsCreator.Run();
    }

    private void MakeFilesWhenUseCarcassAndUseDatabase()
    {
        if (_projectShortName is null)
        {
            Logger.LogError("ProjectShortName is not specified");
            return;
        }

        //მასტერდატას ჩამტვირთავების პროექტის აუცილებელი ფაილები
        //var masterDataRepositoryClassFileName = $"{_projectShortName}MasterDataRepository.cs";
        //Console.WriteLine($"Creating {masterDataRepositoryClassFileName}...");
        //var projectMasterDataRepositoryClassCreator = new ProjectMasterDataRepositoryClassCreator(Logger,
        //    _apiAppCreatorData.RepositoriesProjectData.ProjectFullPath, ProjectName, _projectShortName,
        //    masterDataRepositoryClassFileName);
        //projectMasterDataRepositoryClassCreator.CreateFileStructure();

        //var mdLoaderCreatorInterfaceFileName = $"I{_projectShortName.Capitalize()}MdLoaderCreator.cs";
        //Console.WriteLine($"Creating {mdLoaderCreatorInterfaceFileName}...");
        //var mdLoaderCreatorInterfaceCreator = new MdLoaderCreatorInterfaceCreator(Logger,
        //    _apiAppCreatorData.RepositoriesProjectData.ProjectFullPath, ProjectName, _projectShortName,
        //    mdLoaderCreatorInterfaceFileName);
        //mdLoaderCreatorInterfaceCreator.CreateFileStructure();

        //var masterDataRepoManagerClassFileName = $"{_projectShortName}MasterDataRepoManager.cs";
        //Console.WriteLine($"Creating {masterDataRepoManagerClassFileName}...");
        //var masterDataRepoManagerClassCreator = new MasterDataRepoManagerClassCreator(Logger,
        //    _apiAppCreatorData.RepositoriesProjectData.ProjectFullPath, ProjectName, _projectShortName,
        //    masterDataRepoManagerClassFileName);
        //masterDataRepoManagerClassCreator.CreateFileStructure();

        //var testMdLoaderCreatorClassFileName = $"Test{_projectShortName.Capitalize()}MdLoaderCreator.cs";
        //Console.WriteLine($"Creating {testMdLoaderCreatorClassFileName}...");
        //var testMdLoaderCreatorClassCreator = new TestMdLoaderCreatorClassCreator(Logger,
        //    _apiAppCreatorData.RepositoriesProjectData.ProjectFullPath, ProjectName, _projectShortName,
        //    testMdLoaderCreatorClassFileName);
        //testMdLoaderCreatorClassCreator.CreateFileStructure();

        //var testMdLoaderClassFileName = $"Test{_projectShortName.Capitalize()}MdLoader.cs";
        //Console.WriteLine($"Creating {testMdLoaderClassFileName}...");
        //var testMdLoaderClassCreator = new TestMdLoaderClassCreator(Logger,
        //    _apiAppCreatorData.RepositoriesProjectData.ProjectFullPath, ProjectName, _projectShortName,
        //    testMdLoaderClassFileName);
        //testMdLoaderClassCreator.CreateFileStructure();

        const string assemblyReferenceClassFileName = "AssemblyReference.cs";
        Console.WriteLine($"Creating {assemblyReferenceClassFileName}...");
        var assemblyReferenceClassCreator = new AssemblyReferenceClassCreator(Logger,
            _apiAppCreatorData.RepositoriesProjectData.ProjectFullPath, _apiAppCreatorData.RepositoriesProjectData.ProjectName,
            assemblyReferenceClassFileName);
        assemblyReferenceClassCreator.CreateFileStructure();

        var projectAbstractRepositoryClassFileName = $"{_projectShortName.Capitalize()}AbstractRepository.cs";
        Console.WriteLine($"Creating {projectAbstractRepositoryClassFileName}...");
        var projectAbstractRepositoryClassCreator = new ProjectAbstractRepositoryClassCreator(Logger,
            _apiAppCreatorData.RepositoriesProjectData.ProjectFullPath, ProjectName, _projectShortName,
            projectAbstractRepositoryClassFileName);
        projectAbstractRepositoryClassCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseDatabase(JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys)
    {
        const string assemblyReferenceClassFileName = "AssemblyReference.cs";
        Console.WriteLine($"Creating {assemblyReferenceClassFileName}...");
        var assemblyReferenceClassCreator = new AssemblyReferenceClassCreator(Logger,
            _apiAppCreatorData.DatabaseProjectData.ProjectFullPath, _apiAppCreatorData.DbPartProjectName!,
            assemblyReferenceClassFileName);
        assemblyReferenceClassCreator.CreateFileStructure();

        Console.WriteLine("Creating TestQuery.cs...");
        var testQueryClassCreator = new TestQueryClassCreator(Logger,
            _apiAppCreatorData.DatabaseProjectData.FoldersForCreate["QueryModels"], ProjectName, "TestQuery.cs");
        testQueryClassCreator.CreateFileStructure();

        var databaseProjectInstallersPath = _apiAppCreatorData.DatabaseProjectData.FoldersForCreate["Installers"];

        Console.WriteLine("Creating DatabaseInstaller.cs...");
        var databaseInstallerClassCreator = new DatabaseInstallerClassCreator(Logger, databaseProjectInstallersPath,
            ProjectName, appSettingsJsonJObject, userSecretJsonJObject, forEncodeAppSettingsJsonKeys,
            _apiAppCreatorData.UseCarcass, "DatabaseInstaller.cs");
        databaseInstallerClassCreator.CreateFileStructure();

        //Console.WriteLine($"Creating {Par.ProjectName}DesignTimeDbContextFactory.cs...");
        //var projectDesignTimeDbContextFactoryClassCreator =
        //    new ProjectDesignTimeDbContextFactoryClassCreator(Logger,
        //        _apiAppCreatorData.MainProjectData.ProjectFullPath,
        //        Par.ProjectName, $"{Par.ProjectName}DesignTimeDbContextFactory.cs");
        //projectDesignTimeDbContextFactoryClassCreator.CreateFileStructure();

        //---===libProjectRepositories პროექტის ფაილები===---
        //შეიქმნას ბაზის რეპოზიტორიის შემქმნელი ინტერფეისი
        Console.WriteLine($"Creating I{ProjectName}RepositoryCreatorFabric.cs...");
        var repositoryCreatorFabricInterfaceCreator = new RepositoryCreatorFabricInterfaceCreator(Logger,
            _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"I{ProjectName}RepositoryCreatorFabric.cs");
        repositoryCreatorFabricInterfaceCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორიის შემქმნელი
        Console.WriteLine($"Creating {ProjectName}RepositoryCreatorFabric.cs...");
        var repositoryCreatorFabricCreator = new RepositoryCreatorFabricCreator(Logger,
            _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"{ProjectName}RepositoryCreatorFabric.cs");
        repositoryCreatorFabricCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორია
        Console.WriteLine($"Creating I{ProjectName}Repository.cs...");
        var repositoryInterfaceCreator = new RepositoryInterfaceCreator(Logger,
            _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"I{ProjectName}Repository.cs");
        repositoryInterfaceCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორია
        Console.WriteLine($"Creating {ProjectName}Repository.cs...");
        var repositoryClassCreator = new RepositoryClassCreator(Logger,
            _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, ProjectName,
            $"{ProjectName}Repository.cs");
        repositoryClassCreator.CreateFileStructure();

        //---===ბაზის კონტექსტის პროექტის ფაილები===---
        Console.WriteLine($"Creating {ProjectName}DbContext.cs...");
        CodeCreator dbContextClassCreator = _apiAppCreatorData.UseCarcass
            ? new DbContextForCarcassClassCreator(Logger, _apiAppCreatorData.DatabaseProjectData.ProjectFullPath,
                ProjectName, $"{ProjectName}DbContext.cs")
            : new DbContextClassCreator(Logger, _apiAppCreatorData.DatabaseProjectData.ProjectFullPath, ProjectName,
                $"{ProjectName}DbContext.cs");
        dbContextClassCreator.CreateFileStructure();

        Console.WriteLine("Creating DesignTimeDbContextFactory.cs...");
        var designTimeDbContextFactoryClassCreator = new DesignTimeDbContextFactoryClassCreator(Logger,
            _apiAppCreatorData.DatabaseProjectData.ProjectFullPath, ProjectName, "DesignTimeDbContextFactory.cs");
        designTimeDbContextFactoryClassCreator.CreateFileStructure();

        Console.WriteLine("Creating TestModel.cs...");
        var testModelClassCreator = new TestModelClassCreator(Logger,
            _apiAppCreatorData.DatabaseProjectData.FoldersForCreate["Models"], ProjectName,
            _apiAppCreatorData.UseCarcass, true, "TestModel.cs");
        testModelClassCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseCarcass()
    {
        if (_projectShortName is null)
        {
            Logger.LogError("ProjectShortName is not specified");
            return;
        }

        var installersPath = _apiAppCreatorData.RepositoriesProjectData.FoldersForCreate["Installers"];

        Console.WriteLine("Creating RepositoriesInstaller.cs...");
        var repositoriesInstallerClassCreator = new RepositoriesInstallerClassCreator(Logger, installersPath,
            ProjectName, _projectShortName, "RepositoriesInstaller.cs");
        repositoriesInstallerClassCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseIdentityAndNotUseCarcass(string modelsPath)
    {
        //შეიქმნას პარამეტრების მოდელის კლასი StatProgramAttr.cs
        Console.WriteLine("Creating AppSettings.cs...");
        var appSettingsClassCreator = new AppSettingsClassCreator(Logger, modelsPath, ProjectName, "AppSettings.cs");
        appSettingsClassCreator.CreateFileStructure();
    }

    //შესაქმნელი პროექტების მონაცემების სიის შექმნა
    protected override void PrepareProjectsData()
    {
        AddProject(_apiAppCreatorData.MainProjectData);
        if (!_apiAppCreatorData.UseDatabase)
            return;
        AddProject(_apiAppCreatorData.LibProjectRepositoriesProjectData);
        AddProject(_apiAppCreatorData.DatabaseProjectData);
        AddProject(_apiAppCreatorData.DbMigrationProjectData);
        if (!_apiAppCreatorData.UseCarcass)
            return;
        AddProject(_apiAppCreatorData.RepositoriesProjectData);
        if (_apiAppCreatorData.UseReact)
            AddProject(_apiAppCreatorData.FrontendProjectData);
    }
}