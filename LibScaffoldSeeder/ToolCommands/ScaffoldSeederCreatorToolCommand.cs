using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using DbContextAnalyzer.Domain;
using DbContextAnalyzer.Models;
using LibDatabaseParameters;
using LibDotnetWork;
using LibParameters;
using LibScaffoldSeeder.Models;
using LibSeedCodeCreator;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands;

public sealed class ScaffoldSeederCreatorToolCommand : ToolCommand
{
    private const string ActionDescription = """
                                             This action will do steps:

                                             1. Create Scaffold Seeder Solution
                                             2. scaffold Production Copy Database
                                             3. Create seeder Projects code
                                             4. Create seeder Projects Parameters
                                             5. Run CreateSeederCode
                                             6. Run GetJsonFromScaffoldDb


                                             """;

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger _logger;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ScaffoldSeederCreatorToolCommand(ILogger logger, IHttpClientFactory httpClientFactory, bool useConsole,
        ScaffoldSeederCreatorParameters parameters, IParametersManager parametersManager) : base(logger,
        "Scaffold Seeder Creator", parameters, parametersManager, ActionDescription)
    {
        _logger = logger;
        _useConsole = useConsole;
        _httpClientFactory = httpClientFactory;
    }

    private ScaffoldSeederCreatorParameters Parameters => (ScaffoldSeederCreatorParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var scaffoldSeederDoubleAppCreator =
            new ScaffoldSeederDoubleAppCreator(_logger, _httpClientFactory, _useConsole, Parameters);
        if (!await scaffoldSeederDoubleAppCreator.CreateDoubleApp(cancellationToken))
        {
            StShared.WriteErrorLine("solution does not created", true, _logger);
            return false;
        }

        var gitProjectNames = scaffoldSeederDoubleAppCreator.GitClones.Select(x => x.GitProjectFolderName).ToList();

        if (ParametersManager is null)
        {
            StShared.WriteErrorLine("ParametersManager is null", true, _logger);
            return false;
        }

        var supportToolsParameters = (SupportToolsParameters)ParametersManager.Parameters;

        var project = supportToolsParameters.GetProject(Parameters.ProjectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {Parameters.ProjectName} does not exists", true, _logger);
            return false;
        }

        var haveToSaveSupportToolsParameters = false;

        //გიტის პროექტების გადანახვა, რომლებიც გამოიყენება სკაფოლდინგის ნაწილისთვის
        if (gitProjectNames.Count != project.ScaffoldSeederGitProjectNames.Count ||
            !gitProjectNames.All(project.ScaffoldSeederGitProjectNames.Contains))
        {
            project.ScaffoldSeederGitProjectNames = [];
            project.ScaffoldSeederGitProjectNames.AddRange(gitProjectNames);
            haveToSaveSupportToolsParameters = true;
        }

        if (scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData is null)
            return false;

        if (!ScaffoldProdCopyDatabase(scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData))
            return false;


        const string jsonExt = ".json";

        var seedDbProjectNameUseJsonFilePath = Path.Combine(scaffoldSeederDoubleAppCreator.SolutionSecurityFolderPath,
            $"{Parameters.SeedDbProjectName}{jsonExt}");

        var creatorCreatorParameters = new CreatorCreatorParameters(Parameters.ScaffoldSeederProjectName,
            Parameters.MainDatabaseProjectName, Parameters.ProjectDbContextClassName, Parameters.ProjectShortPrefix,
            Parameters.CreateProjectSeederCodeProjectName,
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath,
                Parameters.CreateProjectSeederCodeProjectName), Parameters.GetJsonFromScaffoldDbProjectName,
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath,
                Parameters.GetJsonFromScaffoldDbProjectName), Parameters.SeedDbProjectName,
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath, Parameters.SeedDbProjectName),
            "ConnectionStringSeed", seedDbProjectNameUseJsonFilePath);

        var creatorCreator = new CreatorCreatorToolCommand(_logger, creatorCreatorParameters);

        if (!await creatorCreator.Run(cancellationToken))
        {
            StShared.WriteErrorLine("Creator code not created", true, _logger);
            return false;
        }


        var seederParameters = new SeederParametersDomain(
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath, Parameters.DataSeedingClassLibProjectName,
                "Json"), Parameters.ProjectSecurityFolderPath, Parameters.LogFolder, Parameters.DevDatabaseDataProvider,
            $"{Parameters.DevDatabaseConnectionString.AddNeedLastPart(';')}Application Name={Parameters.SeedDbProjectName}");

        if (!SaveParameters(seederParameters, seedDbProjectNameUseJsonFilePath,
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.SeedDbProject.ProjectFullPath))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, _logger);
            return false;
        }

        //ბაზაში ინფორმაციის ჩამყრელი პროექტის გზა
        if (string.IsNullOrWhiteSpace(project.SeedProjectFilePath) || project.SeedProjectFilePath !=
            scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.SeedDbProject.ProjectFileFullName)
        {
            project.SeedProjectFilePath = scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.SeedDbProject
                .ProjectFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        //ბაზაში ინფორმაციის ჩამყრელი პროექტის პარამეტრების გზა
        if (string.IsNullOrWhiteSpace(project.SeedProjectParametersFilePath) ||
            project.SeedProjectParametersFilePath != seedDbProjectNameUseJsonFilePath)
        {
            project.SeedProjectParametersFilePath = seedDbProjectNameUseJsonFilePath;
            haveToSaveSupportToolsParameters = true;
        }

        var getJsonParameters = new GetJsonParametersDomain(seederParameters.JsonFolderName, Parameters.LogFolder,
            $"{Parameters.ProdCopyDatabaseConnectionString.AddNeedLastPart(';')}Application Name={Parameters.GetJsonFromScaffoldDbProjectName}");

        var getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName = Path.Combine(
            scaffoldSeederDoubleAppCreator.SolutionSecurityFolderPath,
            $"{Parameters.GetJsonFromScaffoldDbProjectName}{jsonExt}");

        if (!SaveParameters(getJsonParameters, getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName,
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.GetJsonFromProjectDbProject
                    .ProjectFullPath))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, _logger);
            return false;
        }

        //json ფაილების შემქმნელი პროექტის გზა
        if (string.IsNullOrWhiteSpace(project.GetJsonFromScaffoldDbProjectFileFullName) ||
            project.GetJsonFromScaffoldDbProjectFileFullName != scaffoldSeederDoubleAppCreator
                .ScaffoldSeederMainCreatorData.GetJsonFromProjectDbProject.ProjectFileFullName)
        {
            project.GetJsonFromScaffoldDbProjectFileFullName = scaffoldSeederDoubleAppCreator
                .ScaffoldSeederMainCreatorData.GetJsonFromProjectDbProject.ProjectFileFullName;
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
            scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.DbMigrationProject.ProjectFileFullName)
        {
            project.MigrationProjectFilePath = scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData
                .DbMigrationProject.ProjectFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        //FakeHostWebApi პროექტის გზა, რომელიც გამოიყენება მიგრაციის სტარტ პროექტად
        if (string.IsNullOrWhiteSpace(project.MigrationStartupProjectFilePath) ||
            project.MigrationStartupProjectFilePath != scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData
                .FakeHostWebApiProject.ProjectFileFullName)
        {
            project.MigrationStartupProjectFilePath = scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData
                .FakeHostWebApiProject.ProjectFileFullName;
            haveToSaveSupportToolsParameters = true;
        }

        var createProjectSeederCodeParameters = new CreateProjectSeederCodeParametersDomain(
            Parameters.ScaffoldSeederProjectName, Parameters.ProjectShortPrefix, Parameters.LogFolder,
            $"{Parameters.ProdCopyDatabaseConnectionString.AddNeedLastPart(';')}Application Name={Parameters.CreateProjectSeederCodeProjectName}",
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath,
                Parameters.GetJsonFromScaffoldDbProjectName), Parameters.GetJsonFromScaffoldDbProjectName,
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath, Parameters.DataSeedingClassLibProjectName),
            Parameters.DataSeedingClassLibProjectName, Parameters.ExcludesRulesParametersFilePath,
            Parameters.MainDatabaseProjectName, Parameters.ProjectDbContextClassName);

        var createProjectSeederCodeParametersFileFullName = Path.Combine(
            scaffoldSeederDoubleAppCreator.SolutionSecurityFolderPath,
            $"{Parameters.CreateProjectSeederCodeProjectName}{jsonExt}");

        if (!SaveParameters(createProjectSeederCodeParameters, createProjectSeederCodeParametersFileFullName,
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.CreateProjectSeederCodeProject
                    .ProjectFullPath))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, _logger);
            return false;
        }

        if (haveToSaveSupportToolsParameters)
            ParametersManager.Save(supportToolsParameters, "Saved ScaffoldSeederGitProjectNames");

        var dotnetProcessor = new DotnetProcessor(_logger, true);
        if (dotnetProcessor
            .RunToolUsingParametersFile(
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.CreateProjectSeederCodeProject
                    .ProjectFileFullName, createProjectSeederCodeParametersFileFullName).IsSome)
            return false;

        var jsonFromProjectDbProjectGetterParameters = new JsonFromProjectDbProjectGetterParameters(
            scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.GetJsonFromProjectDbProject
                .ProjectFileFullName, getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName);

        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
        var jsonFromProjectDbProjectGetter =
            new JsonFromProjectDbProjectGetter(_logger, jsonFromProjectDbProjectGetterParameters, ParametersManager);
        return await jsonFromProjectDbProjectGetter.Run(cancellationToken);
    }

    private bool ScaffoldProdCopyDatabase(ScaffoldSeederCreatorData scaffoldSeederCreatorData)
    {
        //ბაზის კონტექსტის კლასის სახელი
        var dbScContextName = $"{Parameters.ScaffoldSeederProjectName.Replace('.', '_')}DbScContext";

        var providerPackageName = Parameters.ProdCopyDatabaseDataProvider switch
        {
            EDatabaseProvider.SqlServer => "Microsoft.EntityFrameworkCore.SqlServer",
            EDatabaseProvider.None => null,
            EDatabaseProvider.SqLite => "Microsoft.EntityFrameworkCore.Sqlite",
            EDatabaseProvider.OleDb => "EntityFrameworkCore.Jet",
            _ => null
        };

        if (providerPackageName is not null)
            return ScaffoldDatabase(scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject.ProjectFileFullName,
                Parameters.ProdCopyDatabaseConnectionString,
                scaffoldSeederCreatorData.CreateProjectSeederCodeProject.ProjectFileFullName,
                scaffoldSeederCreatorData.DatabaseScaffoldClassLibProject.ProjectFullPath, providerPackageName,
                dbScContextName);

        StShared.WriteErrorLine("Package name for ProdCopyDatabaseDataProvider does not found", true, _logger);
        return false;
    }

    private bool ScaffoldDatabase(string databaseScaffoldClassLibProjectFileFullName,
        string prodCopyDatabaseConnectionString, string createProjectSeederCodeProjectFileFullName,
        string databaseScaffoldClassLibProjectFullPath, string providerPackageName, string dbScContextName)
    {
        var dotnetProcessor = new DotnetProcessor(_logger, true);
        return dotnetProcessor.EfDatabaseScaffold(databaseScaffoldClassLibProjectFileFullName,
            prodCopyDatabaseConnectionString, providerPackageName, createProjectSeederCodeProjectFileFullName,
            dbScContextName, databaseScaffoldClassLibProjectFullPath).IsNone;
    }


    private bool SaveParameters(object parameters, string saveAsFilePath, string projectFullPath)
    {
        //seederParameters შევინახოთ json-ის სახით პარამეტრების ფოლდერში შესაბამისი პროექტისათვის
        var paramsJsonText = JsonConvert.SerializeObject(parameters, Formatting.Indented);

        //აქ შეიძლება დაშიფვრა დაგვჭირდეს.
        File.WriteAllText(saveAsFilePath, paramsJsonText);

        //launchSettings.json//"SeedGeoModelDb"
        // ReSharper disable once CollectionNeverUpdated.Local
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
}