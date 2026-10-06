using DatabaseTools.DbTools;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Tests.Registry.Mappers;

//a realistic project of the main computer with every field set, two ServerInfos, database parameters and endpoints.
//The ServerInfo keys have the "Server|Env" form and every text is set, so that a project restored from its contract
//serializes the same. All values are made up
internal static class ProjectTestData
{
    public const string Name = "AppFake";
    public const string ProdKey = "PAZISI|Production";
    public const string TestKey = "Merinson|Test";
    public const string Folder = @"D:\1WorkDotnet\AppFake";

    public static ProjectModel NewProject()
    {
        return new ProjectModel
        {
            ProjectType = EProjectType.IsService,
            ProjectGroupName = "Fake Group",
            ProjectDescription = "A made-up project",
            MajorVersion = 2,
            MinorVersion = 7,
            UseAlternativeWebAgent = true,
            ProjectFolderName = Folder,
            SolutionFileName = $@"{Folder}\AppFake\AppFake.slnx",
            EditorConfigPatternName = "default",
            ProjectSecurityFolderPath = @"D:\1WorkDotnet\Security\AppFake",
            MainProjectName = "AppFake",
            ApiContractsProjectName = "AppFakeApiContracts",
            SpaProjectName = "appfake-front",
            ProgramArchiveDateMask = "yyyyMMddHHmmss",
            ProgramArchiveExtension = ".zip",
            ParametersFileDateMask = "yyyyMMdd",
            ParametersFileExtension = ".json",
            MigrationStartupProjectFilePath = $@"{Folder}\FakeHost\FakeHost.csproj",
            DataSeederRulesByTableStartupProjectFilePath = $@"{Folder}\Rules\Rules.csproj",
            OldDataConvertorForDataSeeder = $@"{Folder}\Convertor\Convertor.csproj",
            MigrationProjectFilePath = $@"{Folder}\Migration\Migration.csproj",
            SeedProjectFilePath = $@"{Folder}\Seeder\Seeder.csproj",
            SeedProjectParametersFilePath = @"D:\1WorkDotnet\Security\AppFake\Seeder.json",
            DbContextName = "AppFakeDbContext",
            ProjectShortPrefix = "af",
            ScaffoldSeederProjectName = "AppFakeScaffoldSeeder",
            DbContextProjectName = "AppFakeDb",
            NewDataSeedingClassLibProjectName = "AppFakeNewDataSeeding",
            ExcludesRulesParametersFilePath = @"D:\1WorkDotnet\Security\AppFake\Excludes.json",
            AppSetEnKeysJsonFileName = @"D:\1WorkDotnet\Security\AppFake\AppSetEnKeys.json",
            KeyGuidPart = "00000000-fake-key-guid-part",
            MigrationSqlFilesFolder = $@"{Folder}\Sql",
            PrepareProdCopyDatabaseProjectFilePath = $@"{Folder}\Prepare\Prepare.csproj",
            PrepareProdCopyDatabaseProjectParametersFilePath = @"D:\1WorkDotnet\Security\AppFake\Prepare.json",
            PairedDbObjectsResultFileName = @"D:\1WorkDotnet\Security\AppFake\Paired.json",
            DevDatabaseParameters = NewDatabaseParameters("AppFakeDev"),
            ProdCopyDatabaseParameters = NewDatabaseParameters("AppFakeProdCopy"),
            RedundantFileNames = ["Class1.cs", "WeatherForecast.cs"],
            FrontNpmPackageNames = ["react"],
            Endpoints =
            {
                ["GetItems"] = new EndpointModel
                {
                    EndpointName = "GetItems",
                    EndpointRoute = "items",
                    RequireAuthorization = true,
                    HttpMethod = EHttpMethod.Get,
                    EndpointType = EEndpointType.Query,
                    ReturnType = "List<ItemDto>",
                    SendMessageToCurrentUser = false
                },
                ["SaveItem"] = new EndpointModel
                {
                    EndpointName = "SaveItem",
                    EndpointRoute = "items/save",
                    HttpMethod = EHttpMethod.Post,
                    EndpointType = EEndpointType.Command,
                    ReturnType = "int",
                    SendMessageToCurrentUser = true
                }
            },
            RouteClasses = { ["Items"] = new RouteClassModel { Root = "api", Version = "v2", Base = "items" } },
            GitProjectNames = ["AppFake", "SystemTools"],
            ScaffoldSeederGitProjectNames = ["AppFakeScaffoldSeeder"],
            ServerInfos =
            {
                [ProdKey] = new ServerInfoModel
                {
                    ServerName = "PAZISI",
                    EnvironmentName = "Production",
                    WebAgentNameForCheck = "Pc1.WebAgent",
                    ServerSidePort = 5050,
                    ApiVersionId = "v1",
                    AppSettingsJsonSourceFileName = @"D:\1WorkDotnet\Security\AppFake\appsettings.Production.json",
                    AppSettingsEncodedJsonFileName = $@"{Folder}\Encoded\appsettings.Production.json",
                    ServiceUserName = "appfake",
                    AllowToolsList = [EProjectServerTools.ProgramUpdater, EProjectServerTools.AppSettingsUpdater],
                    CurrentDatabaseParameters = NewDatabaseParameters("AppFake"),
                    NewDatabaseParameters = NewDatabaseParameters("AppFakeNew")
                },
                [TestKey] = new ServerInfoModel
                {
                    ServerName = "Merinson",
                    EnvironmentName = "Test",
                    WebAgentNameForCheck = "Merinson.WebAgent",
                    ApiVersionId = "v1",
                    AppSettingsJsonSourceFileName = @"D:\1WorkDotnet\Security\AppFake\appsettings.Test.json",
                    AppSettingsEncodedJsonFileName = $@"{Folder}\Encoded\appsettings.Test.json",
                    ServiceUserName = "appfake-test",
                    AllowToolsList = [EProjectServerTools.VersionChecker]
                }
            },
            AllowToolsList = [EProjectTools.SeedData, EProjectTools.GenerateApiRoutes]
        };
    }

    public static DatabaseParameters NewDatabaseParameters(string databaseName)
    {
        return new DatabaseParameters
        {
            DbConnectionName = "Dev",
            DatabaseRecoveryModel = EDatabaseRecoveryModel.Simple,
            DbServerFoldersSetName = "Default",
            DatabaseName = databaseName,
            SmartSchemaName = "Daily",
            FileStorageName = "Exchange",
            CommandTimeOut = 600,
            SkipBackupBeforeRestore = true,
            BackupNamePrefix = "pre_",
            DateMask = "yyyyMMdd",
            BackupFileExtension = ".bak",
            BackupNameMiddlePart = "_Full_",
            Compress = true,
            Verify = false,
            BackupType = EBackupType.Diff
        };
    }
}
