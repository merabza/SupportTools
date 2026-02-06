using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.DbContextAnalyzer.Domain;
using AppCliTools.DbContextAnalyzer.Models;
using AppCliTools.LibSeedCodeCreator;
using LibAppProjectCreator;
using LibDotnetWork;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

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
            $"{NamingStats.SeedDbProjectName(Parameters.ScaffoldSeederProjectName)}{jsonExt}");

        var createProjectSeederCodeProjectName =
            NamingStats.CreateProjectSeederCodeProjectName(Parameters.ScaffoldSeederProjectName);

        var getJsonFromScaffoldDbProjectName =
            NamingStats.GetJsonFromScaffoldDbProjectName(Parameters.ScaffoldSeederProjectName);

        var dataSeedingClassLibProjectName =
            NamingStats.DataSeedingClassLibProjectName(Parameters.ScaffoldSeederProjectName);

        var seedDbProjectName = NamingStats.SeedDbProjectName(Parameters.ScaffoldSeederProjectName);

        var creatorCreatorParameters = new CreatorCreatorParameters(Parameters.ScaffoldSeederProjectName,
            Parameters.DbContextProjectName, Parameters.ProjectDbContextClassName, Parameters.ProjectShortPrefix,
            createProjectSeederCodeProjectName,
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath, createProjectSeederCodeProjectName),
            getJsonFromScaffoldDbProjectName,
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath, getJsonFromScaffoldDbProjectName),
            seedDbProjectName, Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath, seedDbProjectName),
            "ConnectionStringSeed", seedDbProjectNameUseJsonFilePath, Parameters.DbContextProjectName,
            Parameters.ProjectDbContextClassName);

        var creatorCreator = new CreatorCreatorToolCommand(_logger, creatorCreatorParameters);

        if (!await creatorCreator.Run(cancellationToken))
        {
            StShared.WriteErrorLine("Creator code not created", true, _logger);
            return false;
        }

        var dataSeedingPackageFolder = NamingStats.DataSeedingPackageFolder(Parameters.ScaffoldSeederProjectName,
            scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.AppCreatorBaseData.WorkPath);

        var seederParameters = new SeederParametersDomain(
            Path.Combine(dataSeedingPackageFolder, dataSeedingClassLibProjectName, "Json"),
            Parameters.ProjectSecurityFolderPath, Parameters.LogFolder, Parameters.DevDatabaseDataProvider,
            $"{Parameters.DevDatabaseConnectionString.AddNeedLastPart(';')}Application Name={seedDbProjectName}",
            Parameters.DevCommandTimeout, Parameters.ExcludesRulesParametersFilePath);

        if (!SaveParameters(seederParameters, seedDbProjectNameUseJsonFilePath,
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.SeedDbProject.ProjectFullPath,
                seedDbProjectName))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, _logger);
            return false;
        }

        var getJsonParameters = new GetJsonParametersDomain(seederParameters.JsonFolderName, Parameters.LogFolder,
            $"{Parameters.ProdCopyDatabaseConnectionString.AddNeedLastPart(';')}Application Name={getJsonFromScaffoldDbProjectName}");

        var getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName = Path.Combine(
            scaffoldSeederDoubleAppCreator.SolutionSecurityFolderPath, $"{getJsonFromScaffoldDbProjectName}{jsonExt}");

        if (!SaveParameters(getJsonParameters, getJsonFromScaffoldDbProjectSeederCodeParametersFileFullName,
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.GetJsonFromProjectDbProject
                    .ProjectFullPath, getJsonFromScaffoldDbProjectName))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, _logger);
            return false;
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
            $"{Parameters.ProdCopyDatabaseConnectionString.AddNeedLastPart(';')}Application Name={createProjectSeederCodeProjectName}",
            Parameters.ProdCommandTimeout,
            $"{Parameters.DevDatabaseConnectionString.AddNeedLastPart(';')}Application Name={createProjectSeederCodeProjectName}",
            Parameters.DevCommandTimeout,
            Path.Combine(scaffoldSeederDoubleAppCreator.SolutionFolderPath, getJsonFromScaffoldDbProjectName),
            getJsonFromScaffoldDbProjectName, Path.Combine(dataSeedingPackageFolder, dataSeedingClassLibProjectName),
            dataSeedingClassLibProjectName, Parameters.ExcludesRulesParametersFilePath, Parameters.DbContextProjectName,
            Parameters.ProjectDbContextClassName);

        var createProjectSeederCodeParametersFileFullName = Path.Combine(
            scaffoldSeederDoubleAppCreator.SolutionSecurityFolderPath,
            $"{createProjectSeederCodeProjectName}{jsonExt}");

        if (!SaveParameters(createProjectSeederCodeParameters, createProjectSeederCodeParametersFileFullName,
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.CreateProjectSeederCodeProject
                    .ProjectFullPath, createProjectSeederCodeProjectName))
        {
            StShared.WriteErrorLine("Parameters does not saved", true, _logger);
            return false;
        }

        if (haveToSaveSupportToolsParameters)
            ParametersManager.Save(supportToolsParameters, "Saved ScaffoldSeederGitProjectNames");

        //აქედან ეშვება კონკრეტული პროექტის მონაცემების ჩამყრელი კოდის შემქმნელი პროგრამა
        //რეალურად ამ პროგრამის საშუალებით ხდება ბაზების გაანალიზება და საჭირო კოდის გენერაცია
        var dotnetProcessor = new DotnetProcessor(_logger, true);
        if (dotnetProcessor
            .RunToolUsingParametersFile(
                scaffoldSeederDoubleAppCreator.ScaffoldSeederMainCreatorData.CreateProjectSeederCodeProject
                    .ProjectFileFullName, createProjectSeederCodeParametersFileFullName).IsSome)
            return false;

        var jsonFromProjectDbProjectGetterParameters = ExternalScaffoldSeedToolParameters.Create(supportToolsParameters,
            Parameters.ProjectName, NamingStats.GetJsonFromScaffoldDbProjectName);
        if (jsonFromProjectDbProjectGetterParameters is null)
        {
            StShared.WriteErrorLine("jsonFromProjectDbProjectGetterParameters is null", true);
            return false;
        }

        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
        var jsonFromProjectDbProjectGetter =
            new ExternalScaffoldSeedToolCommand(_logger, jsonFromProjectDbProjectGetterParameters);
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
        //dotnetProcessor.Restore(databaseScaffoldClassLibProjectFileFullName);
        var restoreResult = dotnetProcessor.Restore(createProjectSeederCodeProjectFileFullName);
        if (restoreResult.IsSome)
            return false;
        return dotnetProcessor.EfDatabaseScaffold(databaseScaffoldClassLibProjectFileFullName,
            prodCopyDatabaseConnectionString, providerPackageName, createProjectSeederCodeProjectFileFullName,
            dbScContextName, databaseScaffoldClassLibProjectFullPath).IsNone;
    }

    private static bool SaveParameters(object parameters, string saveAsFilePath, string projectFullPath,
        string projectName)
    {
        //seederParameters შევინახოთ json-ის სახით პარამეტრების ფოლდერში შესაბამისი პროექტისათვის
        var paramsJsonText = JsonConvert.SerializeObject(parameters, Formatting.Indented);

        //აქ შეიძლება დაშიფვრა დაგვჭირდეს.
        File.WriteAllText(saveAsFilePath, paramsJsonText);

        //launchSettings.json//"SeedGeoModelDb"
        // ReSharper disable once CollectionNeverUpdated.Local
        var seedDbProjectLaunchSettings = new JObject(new JProperty("profiles",
            new JObject(new JProperty(projectName,
                new JObject(new JProperty("commandName", "Project"),
                    new JProperty("commandLineArgs", $"--use \"{saveAsFilePath}\""))))));

        var propertiesFolderPath = Path.Combine(projectFullPath, "Properties");
        StShared.CreateFolder(propertiesFolderPath, true);

        File.WriteAllText(Path.Combine(propertiesFolderPath, "launchSettings.json"),
            seedDbProjectLaunchSettings.ToString());

        return true;
    }
}