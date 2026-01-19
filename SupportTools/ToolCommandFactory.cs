using System.Net.Http;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolCommands;
using LibAppProjectCreator;
using LibAppProjectCreator.Models;
using LibAppProjectCreator.ToolCommands;
using LibCodeGenerator.Models;
using LibCodeGenerator.ToolCommands;
using LibDatabaseWork;
using LibDatabaseWork.Models;
using LibDatabaseWork.ToolCommands;
using LibScaffoldSeeder.Models;
using LibScaffoldSeeder.ToolCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportTools.ToolCommandParameters;
using SupportTools.ToolCommands;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools;

public static class ToolCommandFactory
{
    //public static readonly EProjectTools[] ToolsByProjects =
    //[
    //    EProjectTools.CorrectNewDatabase,
    //    EProjectTools.CreateDevDatabaseByMigration,
    //    EProjectTools.DropDevDatabase,
    //    EProjectTools.JetBrainsCleanupCode,
    //    EProjectTools.JsonFromProjectDbProjectGetter,
    //    EProjectTools.RecreateDevDatabase,
    //    EProjectTools.ScaffoldSeederCreator,
    //    EProjectTools.SeedData,
    //    EProjectTools.PrepareProdCopyDatabase
    //];

    //public static readonly EProjectTools[] ToolsByProjectsAndServers =
    //[
    //    EProjectTools.AppSettingsEncoder,
    //    EProjectTools.AppSettingsInstaller,
    //    EProjectTools.AppSettingsUpdater,
    //    EProjectTools.DevBaseToServerCopier,
    //    EProjectTools.ProgPublisher,
    //    EProjectTools.ProgramInstaller,
    //    EProjectTools.ProgramUpdater,
    //    EProjectTools.ProgRemover,
    //    EProjectTools.ServerBaseToProdCopyCopier,
    //    EProjectTools.ServiceInstallScriptCreator,
    //    EProjectTools.ServiceRemoveScriptCreator,
    //    EProjectTools.ServiceStarter,
    //    EProjectTools.ServiceStopper,
    //    EProjectTools.VersionChecker
    //];

    public static IToolCommand? CreateProjectToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        EProjectTools tool, IParametersManager parametersManager, string projectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return null;
        }

        switch (tool)
        {
            case EProjectTools.CorrectNewDatabase:
                var correctNewDbParameters = CorrectNewDbParameters.Create(logger, supportToolsParameters, projectName);
                if (correctNewDbParameters is not null)
                    return new CorrectNewDatabaseToolCommand(logger, correctNewDbParameters,
                        parametersManager); //ახალი ბაზის 
                StShared.WriteErrorLine("correctNewDbParameters is null", true);
                return null;
            case EProjectTools.CreateDevDatabaseByMigration:
                var dmpCreator =
                    DatabaseMigrationParameters.Create(logger, httpClientFactory, supportToolsParameters, projectName);
                if (dmpCreator is not null)
                    return new DatabaseMigrationCreatorMigrationToolCommand(logger, dmpCreator,
                        parametersManager); //მიგრაციის საშუალებით ცარელა დეველოპერ ბაზის შექმნა
                StShared.WriteErrorLine("dmpCreator is null", true);
                return null;
            case EProjectTools.DropDevDatabase:
                var dmpForDropper =
                    DatabaseMigrationParameters.Create(logger, httpClientFactory, supportToolsParameters, projectName);
                if (dmpForDropper is not null)
                    return new DatabaseDropperMigrationToolCommand(logger, dmpForDropper,
                        parametersManager); //დეველოპერ ბაზის წაშლა
                StShared.WriteErrorLine("dmpForDropper is null", true);
                return null;
            case EProjectTools.JetBrainsCleanupCode
                : //jb cleanupcode solutionFileName.sln -> JetBrain-ის უტილიტის გაშვება პროექტის სოლუშენის ფაილის მითითებით კოდის გასაწმენდად და მოსაწესრიგებლად
                var jetBrainsCleanupCodeRunnerParameters =
                    JetBrainsCleanupCodeRunnerParameters.Create(supportToolsParameters, projectName);
                if (jetBrainsCleanupCodeRunnerParameters is not null)
                    return new JetBrainsCleanupCodeRunnerToolCommand(logger, jetBrainsCleanupCodeRunnerParameters);
                StShared.WriteErrorLine("dataSeederParameters is null", true);
                return null;
            case EProjectTools.JsonFromProjectDbProjectGetter
                : //არსებული პროდაქშენ ბაზის ასლიდან დაამზადებს json ფაილები თავიდან
                var jsonFromProjectDbProjectGetterParameters =
                    ExternalScaffoldSeedToolParameters.Create(supportToolsParameters, projectName,
                        NamingStats.GetJsonFromScaffoldDbProjectName);
                if (jsonFromProjectDbProjectGetterParameters is not null)
                    return new ExternalScaffoldSeedToolCommand(logger, jsonFromProjectDbProjectGetterParameters);
                StShared.WriteErrorLine("jsonFromProjectDbProjectGetterParameters is null", true);
                return null;

            case EProjectTools.RecreateDevDatabase:
                var dmpForReCreator =
                    DatabaseMigrationParameters.Create(logger, httpClientFactory, supportToolsParameters, projectName);
                var correctNewDbParametersForRecreate =
                    CorrectNewDbParameters.Create(logger, supportToolsParameters, projectName);
                if (dmpForReCreator is null)
                {
                    StShared.WriteErrorLine("dmpForReCreator is null", true);
                    return null;
                }

                if (project.DevDatabaseParameters == null)
                {
                    StShared.WriteErrorLine(
                        $"DevDatabaseParameters is not specified for Project with name {projectName}", true);
                    return null;
                }

                var databaseServerConnections =
                    new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);
                var apiClients = new ApiClients(supportToolsParameters.ApiClients);

                if (correctNewDbParametersForRecreate is not null)
                    return new DatabaseReCreatorMigrationToolCommand(logger, dmpForReCreator,
                        project.DevDatabaseParameters, correctNewDbParametersForRecreate, databaseServerConnections,
                        apiClients, httpClientFactory, parametersManager); //დეველოპერ ბაზის წაშლა და თავიდან შექმნა
                StShared.WriteErrorLine("correctNewDbParametersForRecreate is null", true);
                return null;
            case EProjectTools.ScaffoldSeederCreator: //სკაფოლდინგისა და სიდინგის პროექტების შექმნა
                var scaffoldSeederCreatorParameters =
                    ScaffoldSeederCreatorParameters.Create(logger, supportToolsParameters, projectName, useConsole);
                if (scaffoldSeederCreatorParameters is not null)
                    return new ScaffoldSeederCreatorToolCommand(logger, httpClientFactory, true,
                        scaffoldSeederCreatorParameters, parametersManager);
                StShared.WriteErrorLine("scaffoldSeederCreatorParameters is null", true);
                return null;
            case EProjectTools.PrepareProdCopyDatabase: //პროდაქშენ ბაზის ასლის მომზადება სკაფოლდით დამუშავებისათვის
                var prepareProdCopyDatabaseParameters = ExternalScaffoldSeedToolParameters.Create(
                    supportToolsParameters, projectName, null, project.PrepareProdCopyDatabaseProjectFilePath,
                    project.PrepareProdCopyDatabaseProjectParametersFilePath);
                if (prepareProdCopyDatabaseParameters is not null)
                    return new ExternalScaffoldSeedToolCommand(logger, prepareProdCopyDatabaseParameters);
                StShared.WriteErrorLine("dataSeederParameters is null", true);
                return null;
            case EProjectTools.SeedData: //json-ფაილებიდან დეველოპერ ბაზაში ინფორმაციის ჩაყრა
                var dataSeederParameters = ExternalScaffoldSeedToolParameters.Create(supportToolsParameters,
                    projectName, NamingStats.SeedDbProjectName, project.SeedProjectFilePath,
                    project.SeedProjectParametersFilePath);
                if (dataSeederParameters is not null)
                    return new ExternalScaffoldSeedToolCommand(logger, dataSeederParameters);
                StShared.WriteErrorLine("dataSeederParameters is null", true);
                return null;
            case EProjectTools.GenerateApiRoutes:
                var generateApiRoutesParameters =
                    GenerateApiRoutesToolParameters.Create(supportToolsParameters, projectName);
                if (generateApiRoutesParameters is not null)
                    return new GenerateApiRoutesToolCommand(logger, parametersManager, generateApiRoutesParameters);
                StShared.WriteErrorLine("generateApiRoutesParameters is null", true);
                return null;
            //case EProjectTools.AppSettingsInstaller:
            //case EProjectTools.AppSettingsUpdater:
            //case EProjectTools.DevBaseToServerCopier:
            //case EProjectTools.ProgPublisher:
            //case EProjectTools.ProgramInstaller:
            //case EProjectTools.ProgramUpdater:
            //case EProjectTools.ProgRemover:
            //case EProjectTools.ServerBaseToProdCopyCopier:
            //case EProjectTools.ServiceInstallScriptCreator:
            //case EProjectTools.ServiceRemoveScriptCreator:
            //case EProjectTools.ServiceStarter:
            //case EProjectTools.ServiceStopper:
            //case EProjectTools.VersionChecker:
            default:
                StShared.WriteErrorLine("Command tool does not created", true, logger);
                return null;
        }
    }

    public static IToolCommand? CreateProjectServerToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        EProjectServerTools tool, IParametersManager parametersManager, string projectName, ServerInfoModel serverInfo)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return null;
        }

        //შევამოწმოთ პროექტის პარამეტრები
        if (string.IsNullOrWhiteSpace(serverInfo.ServerName))
        {
            StShared.WriteErrorLine("Server name is not specified", true);
            return null;
        }

        //შევამოწმოთ სერვერის პარამეტრები
        var server = supportToolsParameters.GetServerData(serverInfo.ServerName);
        if (server is null)
        {
            StShared.WriteErrorLine($"Server with name {serverInfo.ServerName} not found", true);
            return null;
        }

        switch (tool)
        {
            case EProjectServerTools.AppSettingsEncoder: //  EncodeParameters, //პარამეტრების დაშიფვრა
                //+ EncodeParameters=>GenerateEncodedParametersFile=>UploadParametersToExchange
                var appSettingsEncoderParameters =
                    AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (appSettingsEncoderParameters is not null)
                    return new ApplicationSettingsEncoderToolCommand(logger, appSettingsEncoderParameters,
                        parametersManager);
                StShared.WriteErrorLine("appSettingsEncoderParameters is null", true);
                return null;
            case EProjectServerTools.AppSettingsInstaller: //  InstallParameters, //დაშიფრული პარამეტრების განახლება
                var appSettingsInstallerParameters =
                    AppSettingsInstallerParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (appSettingsInstallerParameters is not null)
                    return new AppSettingsInstallerToolCommand(logger, httpClientFactory, true,
                        appSettingsInstallerParameters, parametersManager);
                StShared.WriteErrorLine("appSettingsInstallerParameters is null", true);
                return null;
            case EProjectServerTools.AppSettingsUpdater
                : //  UpdateParameters, //პარამეტრების დაშიფვრა და დაინსტალირებული პროგრამისთვის ამ დაშიფრული პარამეტრების გადაგზავნა-განახლება
                //+(EncodeParameters=>UploadParameters=>DownloadParameters=>UpdateParameters)
                var appSettingsUpdaterParameters =
                    AppSettingsUpdaterParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (appSettingsUpdaterParameters is not null)
                    return new AppSettingsUpdaterToolCommand(logger, httpClientFactory, appSettingsUpdaterParameters,
                        parametersManager, true);
                StShared.WriteErrorLine("appSettingsUpdaterParameters is null", true);
                return null;
            case EProjectServerTools.DevBaseToServerCopier: //სერვისის გამაჩერებელი სერვერის მხარეს
                if (project.DevDatabaseParameters == null)
                {
                    StShared.WriteErrorLine(
                        $"DevDatabaseParameters is not specified for Project with name {projectName}", true);
                    return null;
                }

                if (serverInfo.NewDatabaseParameters == null)
                {
                    StShared.WriteErrorLine($"NewDatabaseParameters is not specified {serverInfo.ServerName}", true);
                    return null;
                }

                var copyBaseParametersDevToProd = CopyBaseParametersFactory.CreateCopyBaseParameters(logger,
                    httpClientFactory, project.DevDatabaseParameters, serverInfo.NewDatabaseParameters,
                    supportToolsParameters).Result;
                if (copyBaseParametersDevToProd is not null)
                    return new BaseCopierToolCommand(logger, copyBaseParametersDevToProd, parametersManager);
                StShared.WriteErrorLine("copyBaseParametersDevToProd is null", true);
                return null;
            case EProjectServerTools.ProgPublisher: //  Publish, //პროგრამის საინსტალაციო პაკეტის გამზადება
                //+(CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters)

                var programPublisherParameters =
                    ProgramPublisherParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
                if (programPublisherParameters is null)
                {
                    StShared.WriteErrorLine("programPublisherParameters does not created", true);
                    return null;
                }

                var projectForPublish = supportToolsParameters.GetProjectRequired(projectName);

                if (!projectForPublish.IsService)
                    return new ProgramPublisherToolCommand(logger, programPublisherParameters, parametersManager);

                var appSettingsEncoderParametersForPublish =
                    AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);

                if (appSettingsEncoderParametersForPublish != null)
                    return new ServicePublisherToolCommand(logger, programPublisherParameters,
                        appSettingsEncoderParametersForPublish, parametersManager);

                StShared.WriteErrorLine("appSettingsEncoderParametersForPublish does not created", true);
                return null;
            case EProjectServerTools.ProgramInstaller:
                //  InstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამოყენებით პროგრამის დაინსტალირება-განახლება
                //+(DownloadPackage=>UpdateProgram=>DownloadParameters=>UpdateParameters)
                var programInstallerParameters =
                    ProgramInstallerParameters.Create(supportToolsParameters, projectName, serverInfo);

                if (programInstallerParameters is not null)
                    return new ProgramInstallerToolCommand(logger, httpClientFactory, true, programInstallerParameters,
                        parametersManager);

                StShared.WriteErrorLine("programInstallerParameters is null", true);
                return null;
            case EProjectServerTools.ProgramUpdater:
                //  PublishAndInstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამზადება და პროგრამის დაინსტალირება-განახლება
                //+(CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters=>DownloadPackage=>UpdateProgram=>DownloadParameters=>UpdateParameters)

                var projectForUpdate = supportToolsParameters.GetProjectRequired(projectName);

                if (projectForUpdate.IsService)
                {
                    var programServiceUpdaterParameters =
                        ServiceUpdaterParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
                    if (programServiceUpdaterParameters is not null)
                        return new ServiceUpdaterToolCommand(logger, httpClientFactory, programServiceUpdaterParameters,
                            parametersManager, true);
                    StShared.WriteErrorLine("programServiceUpdaterParameters is null", true);
                    return null;
                }

                var programUpdaterParameters =
                    ProgramUpdaterParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
                if (programUpdaterParameters is not null)
                    return new ProgramUpdaterToolCommand(logger, httpClientFactory, programUpdaterParameters,
                        parametersManager, true);
                StShared.WriteErrorLine("programUpdaterParameters is null", true);
                return null;
            case EProjectServerTools.ProgRemover: //  Remove, //პროგრამის წაშლა
                var serviceStartStopParameters =
                    ProgramRemoverParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceStartStopParameters is not null)
                    return new ProgramRemoverToolCommand(logger, httpClientFactory, serviceStartStopParameters,
                        parametersManager, true);
                StShared.WriteErrorLine("serviceStartStopParameters is null", true);
                return null;
            case EProjectServerTools.ServerBaseToProdCopyCopier: //სერვისის გამაჩერებელი სერვერის მხარეს

                if (project.ProdCopyDatabaseParameters == null)
                {
                    StShared.WriteErrorLine(
                        $"ProdCopyDatabaseParameters is not specified for Project with name {projectName}", true);
                    return null;
                }

                if (serverInfo.CurrentDatabaseParameters == null)
                {
                    StShared.WriteErrorLine($"CurrentDatabaseParameters is not specified {serverInfo.ServerName}",
                        true);
                    return null;
                }

                var copyBaseParametersProdToDev = CopyBaseParametersFactory.CreateCopyBaseParameters(logger,
                    httpClientFactory, serverInfo.CurrentDatabaseParameters, project.ProdCopyDatabaseParameters,
                    supportToolsParameters).Result;

                if (copyBaseParametersProdToDev is not null)
                    return new BaseCopierToolCommand(logger, copyBaseParametersProdToDev, parametersManager);
                StShared.WriteErrorLine("copyBaseParametersProdToDev is null", true);
                return null;
            case EProjectServerTools.ServiceInstallScriptCreator:
                var serviceInstallScriptCreatorParameters =
                    ServiceInstallScriptCreatorParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceInstallScriptCreatorParameters is not null)
                    return new ServiceInstallScriptCreatorToolCommand(logger, serviceInstallScriptCreatorParameters,
                        parametersManager);
                StShared.WriteErrorLine("ServiceInstallScriptCreatorParameters is not created", true);
                return null;
            case EProjectServerTools.ServiceRemoveScriptCreator:
                var serviceRemoveScriptCreatorParameters =
                    ServiceRemoveScriptCreatorParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceRemoveScriptCreatorParameters is not null)
                    return new ServiceRemoveScriptCreatorToolCommand(logger, serviceRemoveScriptCreatorParameters,
                        parametersManager);
                StShared.WriteErrorLine("ServiceRemoveScriptCreatorParameters is not created", true);
                return null;
            case EProjectServerTools.ServiceStarter: //სერვისის გამშვები სერვერის მხარეს
                var serviceStartParameters =
                    ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceStartParameters is not null)
                    return new ServiceStarterToolCommand(logger, httpClientFactory, serviceStartParameters,
                        parametersManager, true);
                StShared.WriteErrorLine("serviceStartParameters is null", true);
                return null;
            case EProjectServerTools.ServiceStopper: //სერვისის გამაჩერებელი სერვერის მხარეს
                var serviceStopParameters =
                    ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceStopParameters is not null)
                    return new ServiceStopperToolCommand(logger, httpClientFactory, serviceStopParameters,
                        parametersManager, true);
                StShared.WriteErrorLine("serviceStopParameters is null", true);
                return null;
            case EProjectServerTools.VersionChecker: //სერვისის გამაჩერებელი სერვერის მხარეს
                var checkVersionParameters =
                    CheckVersionParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (checkVersionParameters is not null)
                    return new VersionCheckerToolCommand(logger, httpClientFactory, checkVersionParameters,
                        parametersManager, true);
                StShared.WriteErrorLine("checkVersionParameters is null", true);
                return null;
            //case EProjectTools.RecreateDevDatabase:
            //case EProjectTools.DropDevDatabase:
            //case EProjectTools.CreateDevDatabaseByMigration:
            //case EProjectTools.CorrectNewDatabase:
            //case EProjectTools.ScaffoldSeederCreator:
            //case EProjectTools.JsonFromProjectDbProjectGetter:
            //case EProjectTools.SeedData:
            //case EProjectTools.PrepareProdCopyDatabase:
            //case EProjectTools.JetBrainsCleanupCode:
            default:
                StShared.WriteErrorLine("Command tool does not created", true, logger);
                return null;
        }
    }
}