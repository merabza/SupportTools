using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CliParameters;
using CompressionManagement;
using DbContextAnalyzer.Domain;
using DbContextAnalyzer.Models;
using DbTools;
using FileManagersMain;
using LibAppProjectCreator.Models;
using LibParameters;
using LibScaffoldSeeder.Models;
using LibSeedCodeCreator;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class ScaffoldSeederCreatorToolCommand : ToolCommand
{
    private const string ActionDescription = @"This action will do steps:

1. Create Scaffold Seeder Solution
2. scaffold Production Copy Database
3. Create seeder Projects code
4. Create seeder Projects Parameters
5. Run CreateSeederCode
6. Run GetJsonFromScaffoldDb

";

    public ScaffoldSeederCreatorToolCommand(ILogger logger, bool useConsole, ScaffoldSeederCreatorParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, "Scaffold Seeder Creator", parameters,
        parametersManager, ActionDescription)
    {
    }

    private ScaffoldSeederCreatorParameters Parameters => (ScaffoldSeederCreatorParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //თუ არ არსებობს შეიქმნას დროებითი ფოლდერი სამუშაო ფოლდერის პროექტის ფოლდერში. მაგალითად D:\1WorkScaffoldSeeders\GeoModel\Temp
        const string tempFolderName = "Temp";
        var scaffoldSeederFolderName = $"{Parameters.ScaffoldSeederProjectName}ScaffoldSeeder";
        var scaffoldSeederSecurityFolderName = $"{scaffoldSeederFolderName}.sec";
        var projectWorkFolderPath =
            Path.Combine(Parameters.ScaffoldSeedersWorkFolder, Parameters.ScaffoldSeederProjectName);
        var tempFolderFullName = Path.Combine(projectWorkFolderPath, tempFolderName);
        var scaffoldSeederFolderPath = Path.Combine(projectWorkFolderPath, scaffoldSeederFolderName);
        var solutionFolderPath = Path.Combine(scaffoldSeederFolderPath, scaffoldSeederFolderName);
        var solutionSecurityFolderPath = Path.Combine(projectWorkFolderPath, scaffoldSeederSecurityFolderName);

        if (!Directory.Exists(tempFolderFullName)) Directory.CreateDirectory(tempFolderFullName);

        if (Directory.Exists(scaffoldSeederFolderPath))
            if (!CompressFolder(scaffoldSeederFolderPath, tempFolderFullName))
            {
                StShared.WriteErrorLine($"{scaffoldSeederFolderPath} does not compressed", true, Logger);
                return false;
            }

        if (Directory.Exists(solutionFolderPath))
            Directory.Delete(solutionFolderPath, true);

        if (Directory.Exists(solutionSecurityFolderPath))
        {
            if (!CompressFolder(solutionSecurityFolderPath, tempFolderFullName))
            {
                StShared.WriteErrorLine($"{scaffoldSeederFolderPath} does not compressed", true, Logger);
                return false;
            }

            Directory.Delete(solutionSecurityFolderPath, true);
            //ForceDeleteDirectory(solutionSecurityFolderPath);ამან არ იმუშავა
        }


        var appCreatorParameters = AppProjectCreatorData.Create(Logger, scaffoldSeederFolderName, "",
            ESupportProjectType.ScaffoldSeeder, scaffoldSeederFolderName, projectWorkFolderPath, projectWorkFolderPath,
            Parameters.LogFolder, null, 4);

        if (appCreatorParameters is null)
        {
            Logger.LogError(
                "AppProjectCreatorData does not created for project {Parameters.ScaffoldSeederProjectName} ScaffoldSeeder",
                Parameters.ScaffoldSeederProjectName);
            return false;
        }

        var appCreatorBaseData = AppCreatorBaseData.Create(Logger, appCreatorParameters, false);

        if (appCreatorBaseData is null)
        {
            StShared.WriteErrorLine("Error when creating Scaffold Seeder Solution Parameters", true, Logger);
            return false;
        }

        var scaffoldSeederCreatorData =
            ScaffoldSeederCreatorData.Create(appCreatorBaseData, appCreatorParameters, Parameters);


        var scaffoldSeederSolutionCreator = new ScaffoldSeederSolutionCreator(Logger,
            Parameters, appCreatorParameters, Parameters.GitProjects, Parameters.GitRepos, scaffoldSeederCreatorData);

        if (!scaffoldSeederSolutionCreator.PrepareParameters())
        {
            StShared.WriteErrorLine("Scaffold Seeder Solution Parameters does not created", true, Logger);
            return false;
        }

        //ეს საჭირო აღარ არის, რადგან CreateApp დროს მაინც ხდება ამის გამოძახება
        //if (!scaffoldSeederSolutionCreator.AppGitsSync())
        //{
        //    StShared.WriteErrorLine("Scaffold Seeder Solution git projects does not sync", true, Logger);
        //    return false;
        //}

        if (!scaffoldSeederSolutionCreator.CreateApp())
        {
            StShared.WriteErrorLine("Scaffold Seeder Solution does not created", true, Logger);
            return false;
        }

        var gitProjectNames =
            scaffoldSeederSolutionCreator.GitClones.Select(x => x.GitProjectFolderName).ToList();

        if (ParametersManager is null)
        {
            StShared.WriteErrorLine("ParametersManager is null", true, Logger);
            return false;
        }

        var supportToolsParameters = (SupportToolsParameters)ParametersManager.Parameters;

        var project = supportToolsParameters.GetProject(Parameters.ProjectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {Parameters.ProjectName} does not exists", true, Logger);
            return false;
        }

        var haveToSaveSupportToolsParameters = false;

        //გიტის პროექტების გადანახვა, რომლებიც გამოიყენება სკაფოლდინგის ნაწილისთვის
        if (gitProjectNames.Count != project.ScaffoldSeederGitProjectNames.Count ||
            !gitProjectNames.All(project.ScaffoldSeederGitProjectNames.Contains))
        {
            project.ScaffoldSeederGitProjectNames = new List<string>();
            project.ScaffoldSeederGitProjectNames.AddRange(gitProjectNames);
            haveToSaveSupportToolsParameters = true;
        }


        if (!ScaffoldProdCopyDatabase(scaffoldSeederCreatorData))
            return false;


        const string jsonExt = ".json";

        var seedDbProjectNameUseJsonFilePath =
            Path.Combine(solutionSecurityFolderPath, $"{Parameters.SeedDbProjectName}{jsonExt}");

        var creatorCreatorParameters = new CreatorCreatorParameters(
            Parameters.ScaffoldSeederProjectName, Parameters.MainDatabaseProjectName,
            Parameters.ProjectDbContextClassName,
            Parameters.ProjectShortPrefix,
            Parameters.CreateProjectSeederCodeProjectName,
            Path.Combine(solutionFolderPath, Parameters.CreateProjectSeederCodeProjectName),
            Parameters.GetJsonFromScaffoldDbProjectName,
            Path.Combine(solutionFolderPath, Parameters.GetJsonFromScaffoldDbProjectName),
            Parameters.SeedDbProjectName,
            Path.Combine(solutionFolderPath, Parameters.SeedDbProjectName),
            "ConnectionStringSeed",
            seedDbProjectNameUseJsonFilePath);

        var creatorCreator = new CreatorCreator(Logger, new ParametersManager(null, creatorCreatorParameters));

        if (!creatorCreator.Run())
        {
            StShared.WriteErrorLine("Creator code not created", true, Logger);
            return false;
        }


        var seederParameters = new SeederParametersDomain(
            Path.Combine(solutionFolderPath, Parameters.DataSeedingClassLibProjectName, "Json"),
            Parameters.ProjectSecurityFolderPath, Parameters.LogFolder, Parameters.DevDatabaseDataProvider,
            $"{Parameters.DevDatabaseConnectionString.AddNeedLastPart(';')}Application Name={Parameters.SeedDbProjectName}");

        if (!SaveParameters(seederParameters, seedDbProjectNameUseJsonFilePath,
                scaffoldSeederCreatorData.SeedDbProject.ProjectFullPath))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, Logger);
            return false;
        }

        //ბაზაში ინფორმაციის ჩამყრელი პროექტის გზა
        if (string.IsNullOrWhiteSpace(project.SeedProjectFilePath) ||
            project.SeedProjectFilePath != scaffoldSeederCreatorData.SeedDbProject.ProjectFileFullName)
        {
            project.SeedProjectFilePath = scaffoldSeederCreatorData.SeedDbProject.ProjectFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        //ბაზაში ინფორმაციის ჩამყრელი პროექტის პარამეტრების გზა
        if (string.IsNullOrWhiteSpace(project.SeedProjectParametersFilePath) ||
            project.SeedProjectParametersFilePath != seedDbProjectNameUseJsonFilePath)
        {
            project.SeedProjectParametersFilePath = seedDbProjectNameUseJsonFilePath;
            haveToSaveSupportToolsParameters = true;
        }

        var getJsonParameters = new GetJsonParametersDomain(seederParameters.JsonFolderName,
            Parameters.LogFolder,
            $"{Parameters.ProdCopyDatabaseConnectionString.AddNeedLastPart(';')}Application Name={Parameters.GetJsonFromScaffoldDbProjectName}");

        var getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName = Path.Combine(solutionSecurityFolderPath,
            $"{Parameters.GetJsonFromScaffoldDbProjectName}{jsonExt}");

        if (!SaveParameters(getJsonParameters, getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName,
                scaffoldSeederCreatorData.GetJsonFromProjectDbProject.ProjectFullPath))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, Logger);
            return false;
        }

        //json ფაილების შემქმნელი პროექტის გზა
        if (string.IsNullOrWhiteSpace(project.GetJsonFromScaffoldDbProjectFileFullName) ||
            project.GetJsonFromScaffoldDbProjectFileFullName !=
            scaffoldSeederCreatorData.GetJsonFromProjectDbProject.ProjectFileFullName)
        {
            project.GetJsonFromScaffoldDbProjectFileFullName =
                scaffoldSeederCreatorData.GetJsonFromProjectDbProject.ProjectFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        //json ფაილების შემქმნელი პროექტის პარამეტრების გზა
        if (string.IsNullOrWhiteSpace(project.GetJsonFromScaffoldDbProjectParametersFileFullName) ||
            project.GetJsonFromScaffoldDbProjectParametersFileFullName !=
            getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName)
        {
            project.GetJsonFromScaffoldDbProjectParametersFileFullName =
                getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        //მიგრაციის პროექტის გზის დაფიქსირება
        if (string.IsNullOrWhiteSpace(project.MigrationProjectFilePath) || project.MigrationProjectFilePath !=
            scaffoldSeederCreatorData.DbMigrationProject.ProjectFileFullName)
        {
            project.MigrationProjectFilePath = scaffoldSeederCreatorData.DbMigrationProject.ProjectFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        //FakeHostWebApi პროექტის გზა, რომელიც გამიყენება მიგრაციის სტარტ პროექტად
        if (string.IsNullOrWhiteSpace(project.MigrationStartupProjectFilePath) ||
            project.MigrationStartupProjectFilePath !=
            scaffoldSeederCreatorData.FakeHostWebApiProject.ProjectFileFullName)
        {
            project.MigrationStartupProjectFilePath =
                scaffoldSeederCreatorData.FakeHostWebApiProject.ProjectFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        var createProjectSeederCodeParameters = new CreateProjectSeederCodeParametersDomain(
            Parameters.ScaffoldSeederProjectName, Parameters.ProjectShortPrefix,
            Parameters.LogFolder,
            $"{Parameters.ProdCopyDatabaseConnectionString.AddNeedLastPart(';')}Application Name={Parameters.CreateProjectSeederCodeProjectName}",
            Path.Combine(solutionFolderPath, Parameters.GetJsonFromScaffoldDbProjectName),
            Parameters.GetJsonFromScaffoldDbProjectName,
            Path.Combine(solutionFolderPath, Parameters.DataSeedingClassLibProjectName),
            Parameters.DataSeedingClassLibProjectName,
            Parameters.ExcludesRulesParametersFilePath,
            Parameters.MainDatabaseProjectName,
            Parameters.ProjectDbContextClassName);

        var createProjectSeederCodeParametersFileFullName = Path.Combine(solutionSecurityFolderPath,
            $"{Parameters.CreateProjectSeederCodeProjectName}{jsonExt}");

        if (!SaveParameters(createProjectSeederCodeParameters, createProjectSeederCodeParametersFileFullName,
                scaffoldSeederCreatorData.CreateProjectSeederCodeProject.ProjectFullPath))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, Logger);
            return false;
        }

        if (haveToSaveSupportToolsParameters)
            ParametersManager.Save(supportToolsParameters, "Saved ScaffoldSeederGitProjectNames");

        if (!StShared.RunProcess(true, Logger, "dotnet",
                $"run --project {scaffoldSeederCreatorData.CreateProjectSeederCodeProject.ProjectFileFullName} --use {createProjectSeederCodeParametersFileFullName}"))
            return false;

        var jsonFromProjectDbProjectGetterParameters =
            new JsonFromProjectDbProjectGetterParameters(
                scaffoldSeederCreatorData.GetJsonFromProjectDbProject.ProjectFileFullName,
                getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName);

        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
        var jsonFromProjectDbProjectGetter = new JsonFromProjectDbProjectGetter(Logger, UseConsole,
            jsonFromProjectDbProjectGetterParameters, ParametersManager);
        return jsonFromProjectDbProjectGetter.Run();
    }


    public static void ForceDeleteDirectory(string path)
    {
        var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

        foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
            info.Attributes = FileAttributes.Normal;

        directory.Delete(true);
    }


    private bool ScaffoldProdCopyDatabase(ScaffoldSeederCreatorData scaffoldSeederCreatorData)
    {
        //ბაზის კონტექსტის კლასის სახელი
        var dbScContextName = $"{Parameters.ScaffoldSeederProjectName.Replace('.', '_')}DbScContext";

        var providerPackageName =
            Parameters.ProdCopyDatabaseDataProvider switch
            {
                EDataProvider.Sql => "Microsoft.EntityFrameworkCore.SqlServer",
                EDataProvider.None => null,
                EDataProvider.SqLite => "Microsoft.EntityFrameworkCore.Sqlite",
                _ => null
            };

        if (providerPackageName is null)
        {
            StShared.WriteErrorLine("Package name for ProdCopyDatabaseDataProvider does not found", true, Logger);
            return false;
        }

        if (!ScaffoldDatabase(scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject.ProjectFileFullName,
                Parameters.ProdCopyDatabaseConnectionString,
                scaffoldSeederCreatorData.CreateProjectSeederCodeProject.ProjectFileFullName,
                scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject.ProjectFullPath, providerPackageName,
                dbScContextName))
            return false;
        return true;
    }

    private bool ScaffoldDatabase(string databaseScaffoldClassLibProjectFileFullName,
        string prodCopyDatabaseConnectionString, string createProjectSeederCodeProjectFileFullName,
        string databaseScaffoldClassLibProjectFullPath, string providerPackageName, string dbScContextName)
    {
        return StShared.RunProcess(true, Logger, "dotnet",
            $"ef dbcontext scaffold --project {databaseScaffoldClassLibProjectFileFullName} \"{prodCopyDatabaseConnectionString}\" {providerPackageName} --startup-project {createProjectSeederCodeProjectFileFullName} --context {dbScContextName} --context-dir . --output-dir {Path.Combine(databaseScaffoldClassLibProjectFullPath, "Models")} --force --no-pluralize --no-onconfiguring");
    }


    private bool SaveParameters(object parameters, string saveAsFilePath, string projectFullPath)
    {
        //seederParameters შევინახოთ json-ის სახით პარამეტრების ფოლდერში შესაბამისი პროექტისათვის
        var paramsJsonText = JsonConvert.SerializeObject(parameters, Formatting.Indented);

        //აქ შეიძლება დაშიფვრა დაგვჭირდეს.
        File.WriteAllText(saveAsFilePath, paramsJsonText);

        //launchSettings.json//"SeedGeoModelDb"
        var seedDbProjectLaunchSettings = new JObject(new JProperty("profiles",
            new JObject(new JProperty(Parameters.SeedDbProjectName,
                new JObject(new JProperty("commandName", "Project"),
                    new JProperty("commandLineArgs", $"--use \"{saveAsFilePath}\""))))));

        var propertiesFolderPath = Path.Combine(projectFullPath, "Properties");
        StShared.CreateFolder(propertiesFolderPath, true);

        File.WriteAllText(Path.Combine(propertiesFolderPath, "launchSettings.json"),
            seedDbProjectLaunchSettings.ToString());

        return true;
    }

    private bool CompressFolder(string sourceFolderFullPath, string localPath)
    {
        const string backupFileNameSuffix = ".zip";
        var archiver = ArchiverFabric.CreateArchiverByType(UseConsole, Logger, EArchiveType.ZipClass, null, null,
            backupFileNameSuffix);

        if (archiver is null)
        {
            StShared.WriteErrorLine("archiver does not created", true, Logger);
            return false;
        }

        const string dateMask = "yyyy_MM_dd_HHmmss";
        const string middlePart = "_ScaffoldSeeder_";
        const string tempExtension = ".go!";
        var dir = new DirectoryInfo(sourceFolderFullPath);

        var backupFileNamePrefix = $"{dir.Name}{middlePart}";

        var backupFileName = $"{backupFileNamePrefix}{DateTime.Now.ToString(dateMask)}{backupFileNameSuffix}";
        var backupFileFullName = Path.Combine(localPath, backupFileName);
        var tempFileName = $"{backupFileFullName}{tempExtension}";


        if (!archiver.SourcesToArchive(new[] { sourceFolderFullPath }, tempFileName, Array.Empty<string>()))
        {
            File.Delete(tempFileName);
            return false;
        }

        File.Move(tempFileName, backupFileFullName);

        var localFileManager = FileManagersFabric.CreateFileManager(UseConsole, Logger, localPath);
        //წაიშალოს ადრე შექმნილი დაძველებული ფაილები

        if (localFileManager is null)
        {
            StShared.WriteErrorLine("localFileManager does not created", true, Logger);
            return false;
        }

        localFileManager.RemoveRedundantFiles(backupFileNamePrefix, dateMask, backupFileNameSuffix,
            Parameters.SmartSchemaForLocal);

        return true;
    }
}