using CliParametersDataEdit.Models;
using DbTools;
using LibDatabaseParameters;
using LibFileParameters.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using System.Collections.Generic;
using System.IO;
using SystemToolsShared;

namespace LibAppProjectCreator;

internal sealed class ProjectRecordCreator
{
    private readonly ILogger _logger;
    private readonly string _newProjectKeyGuidPart;
    private readonly string _newProjectName;
    private readonly string _newProjectShortName;

    private readonly IParametersManager _parametersManager;

    public ProjectRecordCreator(ILogger logger, IParametersManager parametersManager, string newProjectName,
        string newProjectShortName, string newProjectKeyGuidPart)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _newProjectName = newProjectName;
        _newProjectShortName = newProjectShortName;
        _newProjectKeyGuidPart = newProjectKeyGuidPart;
    }

    public bool Create()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        var existingProject = supportToolsParameters.GetProject(_newProjectName);

        if (existingProject is not null)
        {
            StShared.WriteErrorLine($"project with name {_newProjectName} already exists and cannot be updated", true,
                _logger);
            return false;
        }

        if (supportToolsParameters.AppProjectCreatorAllParameters is null)
        {
            StShared.WriteErrorLine("supportToolsParameters.AppProjectCreatorAllParameters is null", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters.ProjectsFolderPathReal))
        {
            StShared.WriteErrorLine(
                "supportToolsParameters.AppProjectCreatorAllParameters.ProjectsFolderPathReal is empty", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters.SecretsFolderPathReal))
        {
            StShared.WriteErrorLine(
                "supportToolsParameters.AppProjectCreatorAllParameters.SecretsFolderPathReal is empty", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters.FakeHostProjectName))
        {
            StShared.WriteErrorLine(
                "supportToolsParameters.AppProjectCreatorAllParameters.FakeHostProjectName is empty", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.ScaffoldSeedersWorkFolder is empty", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.SecurityFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.SecurityFolder is empty", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.WorkFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.SecurityFolder is empty", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters.DeveloperDbConnectionName))
        {
            StShared.WriteErrorLine(
                "supportToolsParameters.AppProjectCreatorAllParameters.DeveloperDbConnectionName is empty", true,
                _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters
                .DatabaseExchangeFileStorageName))
        {
            StShared.WriteErrorLine(
                "supportToolsParameters.AppProjectCreatorAllParameters.DatabaseExchangeFileStorageName is empty", true,
                _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters.UseSmartSchema))
        {
            StShared.WriteErrorLine("supportToolsParameters.AppProjectCreatorAllParameters.UseSmartSchema is empty",
                true, _logger);
            return false;
        }


        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters.ProductionServerName))
        {
            StShared.WriteErrorLine(
                "supportToolsParameters.AppProjectCreatorAllParameters.ProductionServerName is empty", true, _logger);
            return false;
        }

        var productionServerName = supportToolsParameters.AppProjectCreatorAllParameters.ProductionServerName;

        var serverData = supportToolsParameters.GetServerData(productionServerName);

        if (serverData is null)
        {
            StShared.WriteErrorLine($"server with name {productionServerName} does not exists", true, _logger);
            return false;
        }


        var developerDbConnectionName =
            supportToolsParameters.AppProjectCreatorAllParameters.DeveloperDbConnectionName;

        DatabaseServerConnections databaseServerConnections = new(supportToolsParameters.DatabaseServerConnections);

        var developerDbConnection =
            databaseServerConnections.GetDatabaseServerConnectionByKey(developerDbConnectionName);

        if (developerDbConnection is null)
        {
            StShared.WriteErrorLine($"Database server connection with name {developerDbConnectionName} does not exists",
                true, _logger);
            return false;
        }

        var databaseExchangeFileStorageName =
            supportToolsParameters.AppProjectCreatorAllParameters.DatabaseExchangeFileStorageName;

        FileStorages fileStorages = new(supportToolsParameters.FileStorages);

        var databaseExchangeFileStorage = fileStorages.GetFileStorageDataByKey(databaseExchangeFileStorageName);

        if (databaseExchangeFileStorage is null)
        {
            StShared.WriteErrorLine($"File Storage with name {databaseExchangeFileStorageName} does not exists", true,
                _logger);
            return false;
        }

        var smartSchemaName = supportToolsParameters.AppProjectCreatorAllParameters.UseSmartSchema;

        SmartSchemas smartSchemas = new(supportToolsParameters.SmartSchemas);

        var smartSchema = smartSchemas.GetSmartSchemaByKey(smartSchemaName);

        if (smartSchema is null)
        {
            StShared.WriteErrorLine($"Smart Schema with name {smartSchemaName} does not exists", true, _logger);
            return false;
        }


        var scaffoldSeederProjectName = $"{_newProjectName}ScaffoldSeeder";
        var fakeHostProjectName = supportToolsParameters.AppProjectCreatorAllParameters.FakeHostProjectName;
        var projectsFolderPathReal = supportToolsParameters.AppProjectCreatorAllParameters.ProjectsFolderPathReal;
        var scaffoldSeedersWorkFolder = supportToolsParameters.ScaffoldSeedersWorkFolder;
        var dbMigrationProjectName = $"{_newProjectName}DbMigration";
        var csProjectExtension = ".csproj";
        //string solutionExtension = ".sln";
        var jsonExtension = ".json";
        var seedProjectName = $"Seed{_newProjectName}Db";
        var getJsonProjectName = $"GetJsonFromScaffold{_newProjectName}Db";
        var scaffoldSeedSecFolderName = $"{_newProjectName}ScaffoldSeeder.sec";
        var dbPartProjectsFolderName = $"{_newProjectName}DbPart";
        var securityFolder = supportToolsParameters.SecurityFolder;
        var appSettingsFileName = $"appsettings{jsonExtension}";
        var productionServerWebAgentName = $"{productionServerName}.WebAgent";
        var productionBaseName = $"{_newProjectName}Prod";
        var tempLocalPath = Path.Combine(supportToolsParameters.WorkFolder, "Bak");

        var newProject = new ProjectModel
        {
            ServiceName = _newProjectName,
            UseAlternativeWebAgent = false,
            ProjectFolderName = Path.Combine(projectsFolderPathReal, _newProjectName),
            SolutionFileName = Path.Combine(projectsFolderPathReal, _newProjectName, _newProjectName,
                $"{_newProjectName}.sln"),
            ProjectSecurityFolderPath =
                Path.Combine(supportToolsParameters.AppProjectCreatorAllParameters.SecretsFolderPathReal,
                    _newProjectName),
            MainProjectName = _newProjectName,
            MigrationStartupProjectFilePath = Path.Combine(scaffoldSeedersWorkFolder, _newProjectName,
                scaffoldSeederProjectName, scaffoldSeederProjectName, fakeHostProjectName,
                $"{fakeHostProjectName}{csProjectExtension}"),
            MigrationProjectFilePath = Path.Combine(scaffoldSeedersWorkFolder, _newProjectName,
                scaffoldSeederProjectName, scaffoldSeederProjectName, dbMigrationProjectName,
                $"{dbMigrationProjectName}{csProjectExtension}"),
            SeedProjectFilePath = Path.Combine(scaffoldSeedersWorkFolder, _newProjectName,
                scaffoldSeederProjectName, scaffoldSeederProjectName, seedProjectName,
                $"{seedProjectName}{csProjectExtension}"),
            SeedProjectParametersFilePath = Path.Combine(scaffoldSeedersWorkFolder, _newProjectName,
                scaffoldSeedSecFolderName, $"{seedProjectName}{csProjectExtension}"),
            GetJsonFromScaffoldDbProjectFileFullName = Path.Combine(scaffoldSeedersWorkFolder, _newProjectName,
                scaffoldSeederProjectName, scaffoldSeederProjectName, getJsonProjectName,
                $"{getJsonProjectName}{csProjectExtension}"),
            GetJsonFromScaffoldDbProjectParametersFileFullName = Path.Combine(scaffoldSeedersWorkFolder,
                _newProjectName, scaffoldSeedSecFolderName, $"{getJsonProjectName}{csProjectExtension}"),
            DbContextName = $"{_newProjectName}DbContext",
            ProjectShortPrefix = _newProjectShortName,
            DbContextProjectName = $"{_newProjectName}Db",
            NewDataSeedingClassLibProjectName = $"{_newProjectName}DbNewDataSeeding",
            ExcludesRulesParametersFilePath = Path.Combine(projectsFolderPathReal, _newProjectName,
                dbPartProjectsFolderName, $"ExcludesRules{jsonExtension}"),
            AppSetEnKeysJsonFileName = Path.Combine(projectsFolderPathReal, _newProjectName, _newProjectName,
                _newProjectName, $"appsetenkeys{jsonExtension}"),
            KeyGuidPart = _newProjectKeyGuidPart,
            DevDatabaseConnectionParameters = new DatabaseConnectionParameters
            {
                DataProvider = EDataProvider.Sql,
                ConnectionString =
                    $"Data Source=(local);Initial Catalog={_newProjectName}ProdCopy;Integrated Security=True;Connect Timeout=15"
            },
            RedundantFileNames = new List<string> { appSettingsFileName },
            GitProjectNames = new List<string>
            {
                "BackendCarcass", "SystemTools", "WebSystemTools", $"{_newProjectName}",
                $"{_newProjectName}ClientApp", "ReactAppCarcass", dbPartProjectsFolderName
            },
            ScaffoldSeederGitProjectNames = new List<string>
            {
                "AppCliTools", "BackendCarcass", "DatabaseTools", "ParametersManagement", "SystemTools",
                "ToolsManagement", "WebSystemTools",
                dbPartProjectsFolderName
            },
            AllowToolsList = new List<ETools>
                { ETools.ScaffoldSeederCreator, ETools.RecreateDevDatabase, ETools.SeedData },
            ServerInfos = new Dictionary<string, ServerInfoModel>
            {
                {
                    productionServerName,
                    new ServerInfoModel
                    {
                        WebAgentNameForCheck = productionServerWebAgentName, ApiVersionId = "1",
                        AppSettingsJsonSourceFileName = Path.Combine(securityFolder, _newProjectName,
                            productionServerName, appSettingsFileName),
                        AppSettingsEncodedJsonFileName = Path.Combine(securityFolder, _newProjectName,
                            productionServerName, $"appsettingsEncoded{jsonExtension}"),
                        ServiceUserName = serverData.FilesUserName,
                        //AllowToolsList = new List<ETools> {ETools.ProgramUpdater}
                        DatabasesExchangeParameters = new DatabasesExchangeParameters
                        {
                            ProductionDbWebAgentName = productionServerWebAgentName,
                            ProductionDbBackupParameters = new DatabaseBackupParametersModel
                            {
                                BackupNamePrefix = $"{productionServerName}_",
                                DateMask = "yyyyMMddHHmmss",
                                BackupFileExtension = ".bak",
                                BackupNameMiddlePart = "_FullDb_",
                                Compress = true,
                                Verify = true,
                                BackupType = EBackupType.Full
                            },
                            CurrentProductionBaseName = productionBaseName,
                            NewProductionBaseName = productionBaseName,
                            ProductionSmartSchemaName = smartSchemaName,
                            ProductionFileStorageName = databaseExchangeFileStorageName,
                            DownloadTempExtension = ".down!",
                            UploadTempExtension = ".up!",
                            ExchangeFileStorageName = databaseExchangeFileStorageName,
                            ExchangeSmartSchemaName = smartSchemaName,
                            LocalPath = tempLocalPath,
                            LocalSmartSchemaName = smartSchemaName,
                            DeveloperFileStorageName = databaseExchangeFileStorageName,
                            DeveloperDbConnectionName = developerDbConnectionName,
                            DeveloperDbBackupParameters = new DatabaseBackupParametersModel
                            {
                                BackupNamePrefix = $"{developerDbConnectionName}_",
                                DateMask = "yyyyMMddHHmmss",
                                BackupFileExtension = ".bak",
                                BackupNameMiddlePart = "_FullDb_",
                                Compress = true,
                                Verify = true,
                                BackupType = EBackupType.Full
                            },
                            DeveloperDbServerSideBackupPath = tempLocalPath,
                            ProductionBaseCopyNameForDeveloperServer = $"{_newProjectName}ProdCopy",
                            DeveloperBaseName = $"{_newProjectName}Development",
                            DeveloperSmartSchemaName = smartSchemaName
                        }
                    }
                }
            }
        };

        supportToolsParameters.Projects.Add(_newProjectName, newProject);

        _parametersManager.Save(supportToolsParameters, $"Project {_newProjectName} saved");

        return true;
    }
}