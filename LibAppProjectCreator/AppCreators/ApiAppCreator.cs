using CodeTools;
using LibAppProjectCreator.CodeCreators;
using LibAppProjectCreator.CodeCreators.CarcassAndDatabase;
using LibAppProjectCreator.CodeCreators.Database;
using LibAppProjectCreator.CodeCreators.GitIgnoreCreators;
using LibAppProjectCreator.CodeCreators.Installers;
using LibAppProjectCreator.CodeCreators.PagesCreators;
using LibAppProjectCreator.JsonCreators;
using LibAppProjectCreator.Models;
using LibAppProjectCreator.React;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SupportToolsData.Models;
using System;
using System.Collections.Generic;
using SystemToolsShared;

namespace LibAppProjectCreator.AppCreators;

public sealed class ApiAppCreator : AppCreatorBase
{
    private readonly ApiAppCreatorData _apiAppCreatorData;
    private readonly Dictionary<string, string> _reactAppTemplates;

    private readonly string _workFolder;

    public ApiAppCreator(ILogger logger, AppProjectCreatorData par, GitProjects gitProjects, GitRepos gitRepos,
        ApiAppCreatorData apiAppCreatorData, string workFolder, Dictionary<string, string> reactAppTemplates) : base(
        logger, par, gitProjects, gitRepos, apiAppCreatorData.AppCreatorBaseData)
    {
        _apiAppCreatorData = apiAppCreatorData;
        _workFolder = workFolder;
        _reactAppTemplates = reactAppTemplates;
    }

    protected override void PrepareFoldersForCheckAndClear()
    {
        base.PrepareFoldersForCheckAndClear();
        FoldersForCheckAndClear.Add(_apiAppCreatorData.ProjectTempPath);
        if (!string.IsNullOrWhiteSpace(_apiAppCreatorData.DbPartPath))
            FoldersForCheckAndClear.Add(_apiAppCreatorData.DbPartPath);
    }

    protected override void PrepareFoldersForCreate()
    {
        base.PrepareFoldersForCreate();
        FoldersForCreate.Add(_apiAppCreatorData.TempPath);
        FoldersForCreate.Add(_apiAppCreatorData.ReactClientPath);
    }

    //protected override string GetSolutionFolderName()
    //{
    //    //ძირითადი პროექტების ფოლდერს ბოლოში ემატება .server
    //    return $"{Par.ProjectName}.server";
    //}

    //პროექტის ტიპისათვის დამახასიათებელი დამატებითი პარამეტრების გამოანგარიშება
    protected override bool PrepareSpecific()
    {
        //if (_apiAppCreatorData.UseCarcass)
        //    AddGitClone(WorkPath, Clones.Instance.BackendCarcass);

        //AddPackage(_apiAppCreatorData.MainProjectData, NuGetPackages.MicrosoftExtensionsLoggingAbstractions);
        if (_apiAppCreatorData.UseIdentity && _apiAppCreatorData.UseCarcass)
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.CarcassIdentity);
        if (_apiAppCreatorData.UseReact)
        {
            AddPackage(_apiAppCreatorData.MainProjectData, NuGetPackages.MicrosoftAspNetCoreSpaServicesExtensions);
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.ReactTools);
        }
        //AddPackage(_apiAppCreatorData.MainProjectData, NuGetPackages.MicrosoftAspNetCoreMvcNewtonsoftJson);

        if (_apiAppCreatorData.UseBackgroundTasks)
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.BackgroundTasksTools);

        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.SystemToolsShared);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.TestToolsMini);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.WebInstallers);

        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.ConfigurationEncrypt);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.SerilogLogger);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.SwaggerTools);
        AddReference(_apiAppCreatorData.MainProjectData, GitProjects.WindowsServiceTools);

        if (_apiAppCreatorData.UseCarcass && _apiAppCreatorData.UseIdentity)
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.ServerCarcass);

        if (_apiAppCreatorData.UseCarcass)
        {
            AddReference(_apiAppCreatorData.DatabaseProjectData, GitProjects.CarcassDb);
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.CarcassIdentity);
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.CarcassRepositories);
            AddReference(_apiAppCreatorData.MainProjectData, GitProjects.ServerCarcassMini);
        }

        if (!_apiAppCreatorData.UseDatabase)
            return true;
        //რეფერენსების სიის შედგენა მთავარი პროექტისათვის
        //AddReference(_apiAppCreatorData.MainProjectData, GitProjects.CliParametersDataEdit);
        AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.DatabaseProjectData);
        AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.LibProjectRepositoriesProjectData);

        if (!_apiAppCreatorData.UseDbPartFolderForDatabaseProjects)
            AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.DbMigrationProjectData);

        //რეფერენსების სიის შედგენა LibProjectRepositories პროექტისათვის
        AddReference(_apiAppCreatorData.LibProjectRepositoriesProjectData,
            _apiAppCreatorData.DatabaseProjectData);

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

        AddReference(_apiAppCreatorData.MasterDataLoadersProjectData, _apiAppCreatorData.DatabaseProjectData);
        AddReference(_apiAppCreatorData.MasterDataLoadersProjectData, GitProjects.CarcassRepositories);
        AddReference(_apiAppCreatorData.MainProjectData, _apiAppCreatorData.MasterDataLoadersProjectData);
        AddReference(_apiAppCreatorData.DatabaseProjectData, GitProjects.CarcassDb);
        return true;
    }

    protected override bool MakeAdditionalFiles()
    {
        var appSettingsJsonJObject = new JObject();
        var userSecretJsonJObject = new JObject();
        //var forEncodeAppSettingsJsonKeys = new Dictionary<string, string>();
        var forEncodeAppSettingsJsonKeys = new List<string>();
        var keyPart1 = Guid.NewGuid().ToString("N");

        //შეიქმნას Program.cs. პროგრამის გამშვები კლასი
        Console.WriteLine("Creating Program.cs...");
        var programClassCreator = new ApiProgramClassCreator(Logger, _apiAppCreatorData.MainProjectData.ProjectFullPath,
            Par.ProjectName, keyPart1, _apiAppCreatorData.UseDatabase, _apiAppCreatorData.UseReact,
            _apiAppCreatorData.UseCarcass, _apiAppCreatorData.UseIdentity, _apiAppCreatorData.UseBackgroundTasks,
            "Program.cs");
        programClassCreator.CreateFileStructure();

        ////შეიქმნას აპლიკაციის მთავარი პარამეტრების შემნახველი კლასი StatProgramAttr.cs
        //Console.WriteLine("Creating StatProgramAttr.cs...");
        //var statProgramAttrClassCreator = new StatProgramAttrClassCreator(Logger,
        //    _apiAppCreatorData.MainProjectData.ProjectFullPath, Par.ProjectName, keyPart1, "StatProgramAttr.cs");
        //statProgramAttrClassCreator.CreateFileStructure();

        var modelsPath = _apiAppCreatorData.MainProjectData.FoldersForCreate["Models"];

        //var installersPath = _apiAppCreatorData.MainProjectData.FoldersForCreate["Installers"];

        ////Installer Interfaces
        //Console.WriteLine("Creating IInstaller.cs...");
        //var installerClassCreator =
        //    new InstallerInterfaceCreator(Logger, installersPath, Par.ProjectName, "IInstaller.cs");
        //installerClassCreator.CreateFileStructure();

        //Console.WriteLine("Creating IAppMiddlewareInstaller.cs...");
        //var appMiddlewareInstallerInterfaceCreator = new AppMiddlewareInstallerInterfaceCreator(Logger,
        //    installersPath, Par.ProjectName, "IAppMiddlewareInstaller.cs");
        //appMiddlewareInstallerInterfaceCreator.CreateFileStructure();

        //Console.WriteLine("Creating IAppUseInstaller.cs...");
        //var appUseInstallerInterfaceCreator = new AppUseInstallerInterfaceCreator(Logger,
        //    installersPath, Par.ProjectName, "IAppUseInstaller.cs");
        //appUseInstallerInterfaceCreator.CreateFileStructure();

        ////Installer Extension Class
        //Console.WriteLine("Creating InstallerExtensions.cs...");
        //var installerExtensionsClassCreator = new InstallerExtensionsClassCreator(Logger,
        //    installersPath, Par.ProjectName, "InstallerExtensions.cs");
        //installerExtensionsClassCreator.CreateFileStructure();

        //Console.WriteLine("Creating ConfigurationEncryptInstaller.cs...");
        //var configurationEncryptInstallerClassCreator =
        //    new ConfigurationEncryptInstallerClassCreator(Logger, installersPath, Par.ProjectName,
        //        "ConfigurationEncryptInstaller.cs");
        //configurationEncryptInstallerClassCreator.CreateFileStructure();

        //Console.WriteLine("Creating ControllersInstaller.cs...");
        //var controllersInstallerClassCreator = new ControllersInstallerClassCreator(Logger,
        //    installersPath, Par.ProjectName, _apiAppCreatorData.UseCarcass, "ControllersInstaller.cs");
        //controllersInstallerClassCreator.CreateFileStructure();

        if (_apiAppCreatorData.UseIdentity && !_apiAppCreatorData.UseCarcass)
            MakeFilesWhenUseIdentityAndNotUseCarcass(modelsPath);

        if (_apiAppCreatorData.UseCarcass)
            MakeFilesWhenUseCarcass();

        if (_apiAppCreatorData.UseDatabase)
            MakeFilesWhenUseDatabase(appSettingsJsonJObject, userSecretJsonJObject, forEncodeAppSettingsJsonKeys);

        if (_apiAppCreatorData.UseCarcass && _apiAppCreatorData.UseDatabase)
            MakeFilesWhenUseCarcassAndUseDatabase();

        if (_apiAppCreatorData.UseIdentity)
            MakeFilesWhenUseIdentity(appSettingsJsonJObject, userSecretJsonJObject, forEncodeAppSettingsJsonKeys);

        //Console.WriteLine("Creating KestrelServerOptionsInstaller.cs...");
        //var kestrelServerOptionsInstallerClassCreator = new KestrelServerOptionsInstallerClassCreator(Logger,
        //    installersPath, Par.ProjectName, "KestrelServerOptionsInstaller.cs");
        //kestrelServerOptionsInstallerClassCreator.CreateFileStructure();

        Console.WriteLine("Creating KestrelOptions...");
        var kestrelOptionsCreator = new KestrelOptionsCreator(appSettingsJsonJObject);
        kestrelOptionsCreator.Run();

        //if (_apiAppCreatorData.UseBackgroundTasks)
        //    MakeFilesWhenUseBackgroundTasks(installersPath);

        if (_apiAppCreatorData.UseReact)
            MakeFilesWhenUseReact();

        //Console.WriteLine("Creating RepositoriesInstaller.cs...");
        //var repositoriesInstallerClassCreator =
        //    new RepositoriesInstallerClassCreator(Logger, installersPath, Par.ProjectName,
        //        "RepositoriesInstaller.cs");
        //repositoriesInstallerClassCreator.CreateFileStructure();

        //Console.WriteLine("Creating SerilogLoggerInstaller.cs...");
        //var serilogLoggerInstallerClassCreator = new SerilogLoggerInstallerClassCreator(Logger, installersPath,
        //    Par.ProjectName, appSettingsJsonJObject, userSecretJsonJObject, forEncodeAppSettingsJsonKeys,
        //    "SerilogLoggerInstaller.cs");
        //serilogLoggerInstallerClassCreator.CreateFileStructure();
        Console.WriteLine("Creating LoggerProperties...");
        var loggerSettingsCreator = new LoggerSettingsCreator(Par.ProjectName, appSettingsJsonJObject,
            userSecretJsonJObject, forEncodeAppSettingsJsonKeys);
        loggerSettingsCreator.Run();


        //Console.WriteLine("Creating SwaggerInstaller.cs...");
        //var swaggerInstallerClassCreator = new SwaggerInstallerClassCreator(Logger, installersPath, Par.ProjectName,
        //    _apiAppCreatorData.UseIdentity, "SwaggerInstaller.cs");
        //swaggerInstallerClassCreator.CreateFileStructure();

        //Console.WriteLine("Creating WindowsServiceInstaller.cs...");
        //var windowsServiceInstallerClassCreator =
        //    new WindowsServiceInstallerClassCreator(Logger, installersPath, Par.ProjectName,
        //        "WindowsServiceInstaller.cs");
        //windowsServiceInstallerClassCreator.CreateFileStructure();

        if (_apiAppCreatorData.UseReact)
        {
            if (string.IsNullOrWhiteSpace(_apiAppCreatorData.ReactTemplateName))
            {
                Logger.LogError("ReactTemplateName does not specified");
                return false;
            }

            var createReactApp = new CreateReactClientApp(Logger, _apiAppCreatorData.TempPath,
                _apiAppCreatorData.ReactClientPath, Par.ProjectName, _apiAppCreatorData.ReactTemplateName, _workFolder,
                _reactAppTemplates);
            if (!createReactApp.Run())
                return false;
        }

        Console.WriteLine("Creating SettingsFiles...");

        var settingsFilesCreator = new SettingsFilesCreator(Logger, _apiAppCreatorData.MainProjectData.ProjectFullPath,
            _apiAppCreatorData.MainProjectData.ProjectFileFullName, appSettingsJsonJObject,
            forEncodeAppSettingsJsonKeys, userSecretJsonJObject, keyPart1);

        if (!settingsFilesCreator.Run())
            return false;

        Console.WriteLine("Creating launchSettings.json...");
        var apiAppLaunchSettingsJsonCreator = new ApiAppLaunchSettingsJsonCreator(_apiAppCreatorData.UseReact,
            Par.ProjectName, _apiAppCreatorData.MainProjectData.ProjectFullPath);
        if (!apiAppLaunchSettingsJsonCreator.Create())
            return false;

        Console.WriteLine("Creating main project .gitignore...");
        var mainProjectGitIgnoreCreator =
            new MainProjectGitIgnoreCreator(Logger, SolutionPath, Par.ProjectName, ".gitignore");
        mainProjectGitIgnoreCreator.CreateFileStructure();

        return true;
    }

    private void MakeFilesWhenUseReact()
    {
        //Console.WriteLine("Creating ReactInstaller.cs...");
        //var reactInstallerClassCreator = new ReactInstallerClassCreator(Logger, installersPath, Par.ProjectName,
        //    _apiAppCreatorData.UseBackgroundTasks, "ReactInstaller.cs");
        //reactInstallerClassCreator.CreateFileStructure();

        var pagesPath = _apiAppCreatorData.MainProjectData.FoldersForCreate["Pages"];

        Console.WriteLine("Creating _ViewImports.cshtml...");
        var viewImportsPageCreator =
            new ViewImportsPageCreator(Logger, pagesPath, Par.ProjectName, "_ViewImports.cshtml");
        viewImportsPageCreator.CreateFileStructure();

        Console.WriteLine("Creating Error.cshtml...");
        var errorPageCreator = new ErrorPageCreator(Logger, pagesPath, "Error.cshtml");
        errorPageCreator.CreateFileStructure();

        Console.WriteLine("Creating Error.cshtml.cs...");
        var errorModelClassCreator = new ErrorModelClassCreator(Logger, pagesPath, Par.ProjectName, "Error.cshtml.cs");
        errorModelClassCreator.CreateFileStructure();
    }

    //private void MakeFilesWhenUseBackgroundTasks(string installersPath)
    //{
    //    Console.WriteLine("Creating ProjectBackgroundTasksQueueInstaller.cs...");
    //    var projectBackgroundTasksQueueInstallerClassCreator = new ProjectBackgroundTasksQueueInstallerClassCreator(
    //        Logger, installersPath, Par.ProjectName, _apiAppCreatorData.UseCarcass,
    //        "ProjectBackgroundTasksQueueInstaller.cs");
    //    projectBackgroundTasksQueueInstallerClassCreator.CreateFileStructure();
    //}

    private static void MakeFilesWhenUseIdentity(JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys)
    {
        //Console.WriteLine("Creating IdentityInstaller.cs...");
        //var identityInstallerClassCreator = new IdentityInstallerClassCreator(Logger, installersPath, Par.ProjectName,
        //    _apiAppCreatorData.UseCarcass, "IdentityInstaller.cs");
        //identityInstallerClassCreator.CreateFileStructure();

        Console.WriteLine("Creating Identity Settings...");
        var identitySettingsCreator = new IdentitySettingsCreator(appSettingsJsonJObject, userSecretJsonJObject,
            forEncodeAppSettingsJsonKeys);
        identitySettingsCreator.Run();
    }

    private void MakeFilesWhenUseCarcassAndUseDatabase()
    {
        //მასტერდატას ჩამტვირთავების პროექტის აუცილებელი ფაილები
        var masterDataRepositoryClassFileName = $"{Par.ProjectShortName}MasterDataRepository.cs";
        Console.WriteLine($"Creating {masterDataRepositoryClassFileName}...");
        var testModelClassCreator = new ProjectMasterDataRepositoryClassCreator(Logger,
            _apiAppCreatorData.MasterDataLoadersProjectData.ProjectFullPath, Par.ProjectName, Par.ProjectShortName,
            masterDataRepositoryClassFileName);
        testModelClassCreator.CreateFileStructure();

        Console.WriteLine("Creating TestQuery.cs...");
        var testQueryClassCreator = new TestQueryClassCreator(Logger,
            _apiAppCreatorData.DatabaseProjectData.FoldersForCreate["QueryModels"], Par.ProjectName,
            "TestQuery.cs");
        testQueryClassCreator.CreateFileStructure();

        var mdLoaderCreatorInterfaceFileName = $"I{Par.ProjectShortName.Capitalize()}MdLoaderCreator.cs";
        Console.WriteLine($"Creating {mdLoaderCreatorInterfaceFileName}...");
        var mdLoaderCreatorInterfaceCreator = new MdLoaderCreatorInterfaceCreator(Logger,
            _apiAppCreatorData.MasterDataLoadersProjectData.ProjectFullPath, Par.ProjectName, Par.ProjectShortName,
            mdLoaderCreatorInterfaceFileName);
        mdLoaderCreatorInterfaceCreator.CreateFileStructure();

        var masterDataRepoManagerClassFileName = $"{Par.ProjectShortName}MasterDataRepoManager.cs";
        Console.WriteLine($"Creating {masterDataRepoManagerClassFileName}...");
        var masterDataRepoManagerClassCreator = new MasterDataRepoManagerClassCreator(Logger,
            _apiAppCreatorData.MasterDataLoadersProjectData.ProjectFullPath, Par.ProjectName, Par.ProjectShortName,
            masterDataRepoManagerClassFileName);
        masterDataRepoManagerClassCreator.CreateFileStructure();

        var testMdLoaderCreatorClassFileName = $"Test{Par.ProjectShortName.Capitalize()}MdLoaderCreator.cs";
        Console.WriteLine($"Creating {testMdLoaderCreatorClassFileName}...");
        var testMdLoaderCreatorClassCreator = new TestMdLoaderCreatorClassCreator(Logger,
            _apiAppCreatorData.MasterDataLoadersProjectData.ProjectFullPath, Par.ProjectName, Par.ProjectShortName,
            testMdLoaderCreatorClassFileName);
        testMdLoaderCreatorClassCreator.CreateFileStructure();

        var testMdLoaderClassFileName = $"Test{Par.ProjectShortName.Capitalize()}MdLoader.cs";
        Console.WriteLine($"Creating {testMdLoaderClassFileName}...");
        var testMdLoaderClassCreator = new TestMdLoaderClassCreator(Logger,
            _apiAppCreatorData.MasterDataLoadersProjectData.ProjectFullPath, Par.ProjectName, Par.ProjectShortName,
            testMdLoaderClassFileName);
        testMdLoaderClassCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseDatabase(JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys)
    {
        var databaseProjectInstallersPath = _apiAppCreatorData.DatabaseProjectData.FoldersForCreate["Installers"];

        Console.WriteLine("Creating DatabaseInstaller.cs...");
        var databaseInstallerClassCreator = new DatabaseInstallerClassCreator(Logger, databaseProjectInstallersPath,
            Par.ProjectName, appSettingsJsonJObject, userSecretJsonJObject, forEncodeAppSettingsJsonKeys,
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
        Console.WriteLine($"Creating I{Par.ProjectName}RepositoryCreatorFabric.cs...");
        var repositoryCreatorFabricInterfaceCreator =
            new RepositoryCreatorFabricInterfaceCreator(Logger,
                _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath,
                Par.ProjectName,
                $"I{Par.ProjectName}RepositoryCreatorFabric.cs");
        repositoryCreatorFabricInterfaceCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორიის შემქმნელი
        Console.WriteLine($"Creating {Par.ProjectName}RepositoryCreatorFabric.cs...");
        var repositoryCreatorFabricCreator =
            new RepositoryCreatorFabricCreator(Logger,
                _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath,
                Par.ProjectName,
                $"{Par.ProjectName}RepositoryCreatorFabric.cs");
        repositoryCreatorFabricCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორია
        Console.WriteLine($"Creating I{Par.ProjectName}Repository.cs...");
        var repositoryInterfaceCreator = new RepositoryInterfaceCreator(Logger,
            _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, Par.ProjectName,
            $"I{Par.ProjectName}Repository.cs");
        repositoryInterfaceCreator.CreateFileStructure();

        //შეიქმნას ბაზის რეპოზიტორია
        Console.WriteLine($"Creating {Par.ProjectName}Repository.cs...");
        var repositoryClassCreator = new RepositoryClassCreator(Logger,
            _apiAppCreatorData.LibProjectRepositoriesProjectData.ProjectFullPath, Par.ProjectName,
            $"{Par.ProjectName}Repository.cs");
        repositoryClassCreator.CreateFileStructure();

        //---===ბაზის კონტექსტის პროექტის ფაილები===---
        Console.WriteLine($"Creating {Par.ProjectName}DbContext.cs...");
        CodeCreator dbContextClassCreator = _apiAppCreatorData.UseCarcass
            ? new DbContextForCarcassClassCreator(Logger, _apiAppCreatorData.DatabaseProjectData.ProjectFullPath,
                Par.ProjectName, $"{Par.ProjectName}DbContext.cs")
            : new DbContextClassCreator(Logger, _apiAppCreatorData.DatabaseProjectData.ProjectFullPath,
                Par.ProjectName, $"{Par.ProjectName}DbContext.cs");
        dbContextClassCreator.CreateFileStructure();

        Console.WriteLine("Creating DesignTimeDbContextFactory.cs...");
        var designTimeDbContextFactoryClassCreator =
            new DesignTimeDbContextFactoryClassCreator(Logger,
                _apiAppCreatorData.DatabaseProjectData.ProjectFullPath, Par.ProjectName,
                "DesignTimeDbContextFactory.cs");
        designTimeDbContextFactoryClassCreator.CreateFileStructure();

        Console.WriteLine("Creating TestModel.cs...");
        var testModelClassCreator = new TestModelClassCreator(Logger,
            _apiAppCreatorData.DatabaseProjectData.FoldersForCreate["Models"], Par.ProjectName,
            _apiAppCreatorData.UseCarcass, "TestModel.cs");
        testModelClassCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseCarcass()
    {
        var installersPath = _apiAppCreatorData.MasterDataLoadersProjectData.FoldersForCreate["Installers"];

        Console.WriteLine("Creating RepositoriesInstaller.cs...");
        var repositoriesInstallerClassCreator = new RepositoriesInstallerClassCreator(Logger, installersPath,
            Par.ProjectName, Par.ProjectShortName, "RepositoriesInstaller.cs");
        repositoriesInstallerClassCreator.CreateFileStructure();
    }

    private void MakeFilesWhenUseIdentityAndNotUseCarcass(string modelsPath)
    {
        //შეიქმნას პარამეტრების მოდელის კლასი StatProgramAttr.cs
        Console.WriteLine("Creating AppSettings.cs...");
        var appSettingsClassCreator =
            new AppSettingsClassCreator(Logger, modelsPath, Par.ProjectName, "AppSettings.cs");
        appSettingsClassCreator.CreateFileStructure();
    }

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
        AddProject(_apiAppCreatorData.MasterDataLoadersProjectData);
    }
}