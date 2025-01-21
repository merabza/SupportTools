using System.Collections.Generic;
using System.IO;
using LibDatabaseParameters;
using LibFileParameters.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator;

internal sealed class ProjectRecordCreator
{
    private readonly ILogger _logger;
    private readonly string _newProjectKeyGuidPart;
    private readonly string _newProjectName;
    private readonly string? _newProjectShortName;

    private readonly IParametersManager _parametersManager;
    private readonly TemplateModel _templateModel;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectRecordCreator(ILogger logger, IParametersManager parametersManager, TemplateModel templateModel,
        string newProjectName, string? newProjectShortName, string newProjectKeyGuidPart)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _templateModel = templateModel;
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

        if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters.ProductionEnvironmentName))
        {
            StShared.WriteErrorLine(
                "supportToolsParameters.AppProjectCreatorAllParameters.ProductionEnvironmentName is empty", true,
                _logger);
            return false;
        }

        var productionEnvironmentName = supportToolsParameters.AppProjectCreatorAllParameters.ProductionEnvironmentName;

        var serverData = supportToolsParameters.GetServerData(productionServerName);

        if (serverData is null)
        {
            StShared.WriteErrorLine($"server with name {productionServerName} is not exists 1", true, _logger);
            return false;
        }


        var developerDbConnectionName = supportToolsParameters.AppProjectCreatorAllParameters.DeveloperDbConnectionName;

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
        const string csProjectExtension = ".csproj";
        const string jsonExtension = ".json";
        var seedProjectName = $"Seed{_newProjectName}Db";
        var getJsonProjectName = $"GetJsonFromScaffold{_newProjectName}Db";
        var scaffoldSeedSecFolderName = $"{_newProjectName}ScaffoldSeeder.sec";
        var dbPartProjectsFolderName = $"{_newProjectName}DbPart";
        var securityFolder = supportToolsParameters.SecurityFolder;
        const string appSettingsFileName = $"appsettings{jsonExtension}";
        var productionServerWebAgentName = $"{productionServerName}.WebAgent";
        //var productionBaseName = $"{_newProjectName}Prod";
        //var tempLocalPath = Path.Combine(supportToolsParameters.WorkFolder, "Bak");

        List<string> redundantFileNames =
            _templateModel.SupportProjectType == ESupportProjectType.Api ? [appSettingsFileName] : [];

        var serverInfos = new Dictionary<string, ServerInfoModel>();

        if (!_templateModel.UseMenu)
        {
            var serverInfo = CreateServerInfo(productionServerName, productionEnvironmentName,
                productionServerWebAgentName, securityFolder, appSettingsFileName, jsonExtension, serverData);
            serverInfos = new Dictionary<string, ServerInfoModel> { { serverInfo.GetItemKey(), serverInfo } };
        }

        List<string> gitProjectNames = ["SystemTools", _newProjectName];
        if (_templateModel.UseMenu)
        {
            gitProjectNames.Add("ToolsManagement");
            gitProjectNames.Add("ParametersManagement");
            gitProjectNames.Add("AppCliTools");
        }

        if (_templateModel.UseDbPartFolderForDatabaseProjects)
            gitProjectNames.Add(dbPartProjectsFolderName);
        if (_templateModel.UseCarcass)
            gitProjectNames.Add("BackendCarcass");
        if (_templateModel.UseDatabase)
            gitProjectNames.Add("DatabaseTools");
        if (_templateModel.UseReact)
        {
            gitProjectNames.Add("ReactAppCarcass");
            gitProjectNames.Add("WebSystemTools");
        }

        var newProject = new ProjectModel
        {
            IsService = _templateModel.SupportProjectType == ESupportProjectType.Api,
            UseAlternativeWebAgent = false,
            ProjectFolderName = Path.Combine(projectsFolderPathReal, _newProjectName),
            SolutionFileName =
                Path.Combine(projectsFolderPathReal, _newProjectName, _newProjectName, $"{_newProjectName}.sln"),
            ProjectSecurityFolderPath =
                Path.Combine(supportToolsParameters.AppProjectCreatorAllParameters.SecretsFolderPathReal,
                    _newProjectName),
            MainProjectName = _newProjectName,
            MigrationStartupProjectFilePath =
                Path.Combine(scaffoldSeedersWorkFolder, _newProjectName, scaffoldSeederProjectName,
                    scaffoldSeederProjectName, fakeHostProjectName, $"{fakeHostProjectName}{csProjectExtension}"),
            MigrationProjectFilePath =
                Path.Combine(scaffoldSeedersWorkFolder, _newProjectName, scaffoldSeederProjectName,
                    scaffoldSeederProjectName, dbMigrationProjectName, $"{dbMigrationProjectName}{csProjectExtension}"),
            SeedProjectFilePath =
                _templateModel is { UseDatabase: true, UseCarcass: true }
                    ? Path.Combine(scaffoldSeedersWorkFolder, _newProjectName, scaffoldSeederProjectName,
                        scaffoldSeederProjectName, seedProjectName, $"{seedProjectName}{csProjectExtension}")
                    : null,
            SeedProjectParametersFilePath =
                _templateModel is { UseDatabase: true, UseCarcass: true }
                    ? Path.Combine(scaffoldSeedersWorkFolder, _newProjectName, scaffoldSeedSecFolderName,
                        $"{seedProjectName}{csProjectExtension}")
                    : null,
            GetJsonFromScaffoldDbProjectFileFullName =
                _templateModel is { UseDatabase: true, UseCarcass: true }
                    ? Path.Combine(scaffoldSeedersWorkFolder, _newProjectName, scaffoldSeederProjectName,
                        scaffoldSeederProjectName, getJsonProjectName, $"{getJsonProjectName}{csProjectExtension}")
                    : null,
            GetJsonFromScaffoldDbProjectParametersFileFullName =
                _templateModel is { UseDatabase: true, UseCarcass: true }
                    ? Path.Combine(scaffoldSeedersWorkFolder, _newProjectName, scaffoldSeedSecFolderName,
                        $"{getJsonProjectName}{csProjectExtension}")
                    : null,
            DbContextName = $"{_newProjectName}DbContext",
            ProjectShortPrefix = _newProjectShortName,
            DbContextProjectName =
                _templateModel is { UseDatabase: true, UseCarcass: true } ? $"{_newProjectName}Db" : null,
            NewDataSeedingClassLibProjectName =
                _templateModel is { UseDatabase: true, UseCarcass: true } ? $"{_newProjectName}DbNewDataSeeding" : null,
            ExcludesRulesParametersFilePath =
                _templateModel is { UseDatabase: true, UseCarcass: true }
                    ? Path.Combine(projectsFolderPathReal, _newProjectName, dbPartProjectsFolderName,
                        $"ExcludesRules{jsonExtension}")
                    : null,
            AppSetEnKeysJsonFileName =
                _templateModel.SupportProjectType == ESupportProjectType.Api
                    ? Path.Combine(projectsFolderPathReal, _newProjectName, _newProjectName, _newProjectName,
                        $"appsetenkeys{jsonExtension}")
                    : null,
            KeyGuidPart = _newProjectKeyGuidPart,
            DevDatabaseParameters = new DatabaseParameters
            {
                //DataProvider = EDataProvider.Sql,
                //ბაზასთან დაკავშირების პარამეტრი უნდა ავიღოთ პრექტის შემქმნელის პარამეტრებიდან
                DbConnectionName = supportToolsParameters.AppProjectCreatorAllParameters.DeveloperDbConnectionName,
                DatabaseName = $"{_newProjectName}ProdCopy"
            },
            RedundantFileNames = redundantFileNames,
            GitProjectNames = gitProjectNames,
            ScaffoldSeederGitProjectNames = _templateModel is { UseDatabase: true, UseCarcass: true }
                ?
                [
                    "AppCliTools", "BackendCarcass", "DatabaseTools", "ParametersManagement", "SystemTools",
                    "ToolsManagement", "WebSystemTools",
                    dbPartProjectsFolderName
                ]
                : [],
            AllowToolsList = _templateModel is { UseDatabase: true, UseCarcass: true }
                ? [ETools.ScaffoldSeederCreator, ETools.RecreateDevDatabase, ETools.SeedData]
                : [ETools.CreateDevDatabaseByMigration, ETools.RecreateDevDatabase, ETools.JetBrainsCleanupCode],
            ServerInfos = serverInfos
        };

        supportToolsParameters.Projects.Add(_newProjectName, newProject);

        _parametersManager.Save(supportToolsParameters, $"Project {_newProjectName} saved");

        return true;
    }

    private ServerInfoModel CreateServerInfo(string productionServerName, string productionEnvironmentName,
        string productionServerWebAgentName, string securityFolder, string appSettingsFileName, string jsonExtension,
        ServerDataModel serverData)
    {
        var serverInfo = new ServerInfoModel
        {
            ServerName = productionServerName,
            EnvironmentName = productionEnvironmentName,
            WebAgentNameForCheck = productionServerWebAgentName,
            ApiVersionId = "v1",
            AppSettingsJsonSourceFileName =
                Path.Combine(securityFolder, _newProjectName, productionServerName, appSettingsFileName),
            AppSettingsEncodedJsonFileName =
                Path.Combine(securityFolder, _newProjectName, productionServerName,
                    $"appsettingsEncoded{jsonExtension}"),
            ServiceUserName = serverData.FilesUserName
        };
        return serverInfo;
    }
}