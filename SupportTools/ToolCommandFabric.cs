using LibAppInstallWork.Models;
using LibAppInstallWork.ToolCommands;
using LibAppProjectCreator.Models;
using LibAppProjectCreator.ToolCommands;
using LibDatabaseWork;
using LibDatabaseWork.Models;
using LibDatabaseWork.ToolCommands;
using LibParameters;
using LibScaffoldSeeder.Models;
using LibScaffoldSeeder.ToolCommands;
using Microsoft.Extensions.Logging;
using SupportTools.ToolCommandParameters;
using SupportTools.ToolCommands;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools;

public static class ToolCommandFabric
{
    public static readonly ETools[] ToolsByProjects =
    {
        ETools.CorrectNewDatabase,
        ETools.CreateDevDatabaseByMigration,
        ETools.DropDevDatabase,
        ETools.JetBrainsCleanupCode,
        ETools.JsonFromProjectDbProjectGetter,
        ETools.RecreateDevDatabase,
        ETools.ScaffoldSeederCreator,
        ETools.SeedData,
    };

    public static readonly ETools[] ToolsByProjectsAndServers =
    {
        ETools.AppSettingsEncoder,
        ETools.AppSettingsInstaller,
        ETools.AppSettingsUpdater,
        ETools.DevBaseToProdCopier,
        ETools.InstallScriptCreator,
        ETools.ProdBaseToDevCopier,
        ETools.ProgPublisher,
        ETools.ProgramInstaller,
        ETools.ProgramUpdater,
        ETools.ProgRemover,
        ETools.ServiceStarter,
        ETools.ServiceStopper,
        ETools.VersionChecker,
    };

    public static IToolCommand? Create(ILogger logger, ETools tool, IParametersManager parametersManager,
        string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        switch (tool)
        {
            case ETools.CorrectNewDatabase:
                var correctNewDbParameters =
                    CorrectNewDbParameters.Create(logger, supportToolsParameters, projectName);
                if (correctNewDbParameters is not null)
                    return new CorrectNewDatabase(logger, true, correctNewDbParameters,
                        parametersManager); //ახალი ბაზის 
                StShared.WriteErrorLine("correctNewDbParameters is null", true);
                return null;
            case ETools.CreateDevDatabaseByMigration:
                var dmpCreator =
                    DatabaseMigrationParameters.Create(logger, supportToolsParameters, projectName);
                if (dmpCreator is not null)
                    return new DatabaseMigrationCreator(logger, true, dmpCreator,
                        parametersManager); //მიგრაციის საშუალებით ცარელა დეველოპერ ბაზის შექმნა
                StShared.WriteErrorLine("dmpCreator is null", true);
                return null;
            case ETools.DropDevDatabase:
                var dmpForDropper =
                    DatabaseMigrationParameters.Create(logger, supportToolsParameters, projectName);
                if (dmpForDropper is not null)
                    return new DatabaseDropper(logger, true, dmpForDropper, parametersManager); //დეველოპერ ბაზის წაშლა
                StShared.WriteErrorLine("dmpForDropper is null", true);
                return null;
            case ETools.JetBrainsCleanupCode
                : //jb cleanupcode solutionFileName.sln -> JetBrain-ის უტილიტის გაშვება პროექტის სოლუშენის ფაილის მითითებით კოდის გასაწმენდად და მოსაწესრიგებლად
                var jetBrainsCleanupCodeRunnerParameters =
                    JetBrainsCleanupCodeRunnerParameters.Create(supportToolsParameters, projectName);
                if (jetBrainsCleanupCodeRunnerParameters is not null)
                    return new JetBrainsCleanupCodeRunner(logger, true, jetBrainsCleanupCodeRunnerParameters);
                StShared.WriteErrorLine("dataSeederParameters is null", true);
                return null;
            case ETools.JsonFromProjectDbProjectGetter
                : //არსებული პროდაქშენ ბაზის ასლიდან დაამზადებს json ფაილები თავიდან
                var jsonFromProjectDbProjectGetterParameters =
                    JsonFromProjectDbProjectGetterParameters.Create(supportToolsParameters, projectName);
                //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით json ფაილები
                if (jsonFromProjectDbProjectGetterParameters is not null)
                    return new JsonFromProjectDbProjectGetter(logger, true, jsonFromProjectDbProjectGetterParameters,
                        parametersManager);
                StShared.WriteErrorLine("jsonFromProjectDbProjectGetterParameters is null", true);
                return null;
            case ETools.RecreateDevDatabase:
                var dmpForReCreator =
                    DatabaseMigrationParameters.Create(logger, supportToolsParameters, projectName);
                var correctNewDbParametersForRecreate =
                    CorrectNewDbParameters.Create(logger, supportToolsParameters, projectName);
                if (dmpForReCreator is null)
                {
                    StShared.WriteErrorLine("dmpForReCreator is null", true);
                    return null;
                }

                if (correctNewDbParametersForRecreate is not null)
                    return new DatabaseReCreator(logger, true, dmpForReCreator, correctNewDbParametersForRecreate,
                        parametersManager); //დეველოპერ ბაზის წაშლა და თავიდან შექმნა
                StShared.WriteErrorLine("correctNewDbParametersForRecreate is null", true);
                return null;
            case ETools.ScaffoldSeederCreator: //სკაფოლდინგისა და სიდინგის პროექტების შექმნა
                var scaffoldSeederCreatorParameters =
                    ScaffoldSeederCreatorParameters.Create(logger, supportToolsParameters, projectName);
                if (scaffoldSeederCreatorParameters is not null)
                    return new ScaffoldSeederCreatorToolCommand(logger, true, scaffoldSeederCreatorParameters,
                        parametersManager);
                StShared.WriteErrorLine("scaffoldSeederCreatorParameters is null", true);
                return null;
            case ETools.SeedData: //json-ფაილებიდან დეველოპერ ბაზაში ინფორმაციის ჩაყრა
                var dataSeederParameters =
                    DataSeederParameters.Create(supportToolsParameters, projectName);
                if (dataSeederParameters is not null)
                    return new DataSeeder(logger, true, dataSeederParameters);
                StShared.WriteErrorLine("dataSeederParameters is null", true);
                return null;
            case ETools.InstallScriptCreator:
            case ETools.AppSettingsEncoder:
            case ETools.AppSettingsInstaller:
            case ETools.AppSettingsUpdater:
            case ETools.ProgPublisher:
            case ETools.ProgramInstaller:
            case ETools.ProgramUpdater:
            case ETools.ProgRemover:
            case ETools.ServiceStarter:
            case ETools.ServiceStopper:
            case ETools.VersionChecker:
            case ETools.ProdBaseToDevCopier:
            case ETools.DevBaseToProdCopier:
            default:
                StShared.WriteErrorLine("Command tool does not created", true, logger);
                return null;
        }
    }

    public static IToolCommand? Create(ILogger logger, ETools tool, IParametersManager parametersManager,
        string projectName, ServerInfoModel serverInfo)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        switch (tool)
        {
            case ETools.AppSettingsEncoder: //  EncodeParameters, //პარამეტრების დაშიფვრა
                //+ EncodeParameters=>GenerateEncodedParametersFile=>UploadParametersToExchange
                var appSettingsEncoderParameters =
                    AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (appSettingsEncoderParameters is not null)
                    return new ApplicationSettingsEncoder(logger, true, appSettingsEncoderParameters,
                        parametersManager);
                StShared.WriteErrorLine("appSettingsEncoderParameters is null", true);
                return null;
            case ETools.AppSettingsInstaller: //  InstallParameters, //დაშიფრული პარამეტრების განახლება
                var appSettingsInstallerParameters =
                    AppSettingsInstallerParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (appSettingsInstallerParameters is not null)
                    return new AppSettingsInstaller(logger, true, appSettingsInstallerParameters, parametersManager);
                StShared.WriteErrorLine("appSettingsInstallerParameters is null", true);
                return null;
            case ETools.AppSettingsUpdater
                : //  UpdateParameters, //პარამეტრების დაშიფვრა და დაინსტალირებული პროგრამისთვის ამ დაშიფრული პარამეტრების გადაგზავნა-განახლება
                //+(EncodeParameters=>UploadParameters=>DownloadParameters=>UpdateParameters)
                var appSettingsUpdaterParameters =
                    AppSettingsUpdaterParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (appSettingsUpdaterParameters is not null)
                    return new AppSettingsUpdater(logger, true, appSettingsUpdaterParameters, parametersManager);
                StShared.WriteErrorLine("appSettingsUpdaterParameters is null", true);
                return null;
            case ETools.DevBaseToProdCopier: //სერვისის გამაჩერებელი სერვერის მხარეს
                var copyBaseParametersDevToProd =
                    CopyBaseParametersFabric.CreateCopyBaseParameters(logger, false, supportToolsParameters,
                        projectName, serverInfo);
                if (copyBaseParametersDevToProd is not null)
                    return new BaseCopier(logger, true, copyBaseParametersDevToProd, parametersManager);
                StShared.WriteErrorLine("copyBaseParametersDevToProd is null", true);
                return null;
            case ETools.InstallScriptCreator:
                var installScriptCreatorParameters =
                    InstallScriptCreatorParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
                if (installScriptCreatorParameters is null)
                {
                    StShared.WriteErrorLine("installScriptCreatorParameters does not created", true);
                    return null;
                }

                if (installScriptCreatorParameters != null)
                    return new InstallScriptCreator(logger, true, installScriptCreatorParameters,
                        parametersManager);

                StShared.WriteErrorLine("installScriptParametersForPublish does not created", true);
                return null;
            case ETools.ProdBaseToDevCopier: //სერვისის გამაჩერებელი სერვერის მხარეს
                var copyBaseParametersProdToDev =
                    CopyBaseParametersFabric.CreateCopyBaseParameters(logger, true, supportToolsParameters, projectName,
                        serverInfo);
                if (copyBaseParametersProdToDev is not null)
                    return new BaseCopier(logger, true, copyBaseParametersProdToDev, parametersManager);
                StShared.WriteErrorLine("copyBaseParametersProdToDev is null", true);
                return null;
            case ETools.ProgPublisher: //  Publish, //პროგრამის საინსტალაციო პაკეტის გამზადება
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
                    return new ProgramPublisher(logger, true, programPublisherParameters, parametersManager);

                var appSettingsEncoderParametersForPublish =
                    AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);

                if (appSettingsEncoderParametersForPublish != null)
                    return new ServicePublisher(logger, true, programPublisherParameters,
                        appSettingsEncoderParametersForPublish, parametersManager);

                StShared.WriteErrorLine("appSettingsEncoderParametersForPublish does not created", true);
                return null;
            case ETools.ProgramInstaller
                : //  InstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამოყენებით პროგრამის დაინსტალირება-განახლება
                //+(DownloadPackage=>UpdateProgram=>DownloadParameters=>UpdateParameters)
                //ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);
                var programInstallerParameters =
                    ProgramInstallerParameters.Create(supportToolsParameters, projectName, serverInfo);

                if (programInstallerParameters is not null)
                    return new ProgramInstaller(logger, true, programInstallerParameters, parametersManager);

                StShared.WriteErrorLine("programInstallerParameters is null", true);
                return null;
            case ETools.ProgramUpdater:
                //  PublishAndInstallUpdate, //პროგრამის საინსტალაციო პაკეტის გამზადება და პროგრამის დაინსტალირება-განახლება
                //+(CreatePackage=>UploadPackage=>EncodeParameters=>UploadParameters=>DownloadPackage=>UpdateProgram=>DownloadParameters=>UpdateParameters)

                var projectForUpdate = supportToolsParameters.GetProjectRequired(projectName);

                if (projectForUpdate.IsService)
                {
                    var programServiceUpdaterParameters =
                        ServiceUpdaterParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
                    if (programServiceUpdaterParameters is not null)
                        return new ServiceUpdater(logger, true, programServiceUpdaterParameters,
                            parametersManager);
                    StShared.WriteErrorLine("programServiceUpdaterParameters is null", true);
                    return null;
                }

                var programUpdaterParameters =
                    ProgramUpdaterParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
                if (programUpdaterParameters is not null)
                    return new ProgramUpdater(logger, true, programUpdaterParameters, parametersManager);
                StShared.WriteErrorLine("programUpdaterParameters is null", true);
                return null;
            case ETools.ProgRemover: //  Remove, //პროგრამის წაშლა
                var serviceStartStopParameters =
                    ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceStartStopParameters is not null)
                    return new ProgramRemover(logger, true, serviceStartStopParameters, parametersManager);
                StShared.WriteErrorLine("serviceStartStopParameters is null", true);
                return null;
            case ETools.ServiceStarter: //სერვისის გამშვები სერვერის მხარეს
                var serviceStartParameters =
                    ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceStartParameters is not null)
                    return new ServiceStarter(logger, true, serviceStartParameters, parametersManager);
                StShared.WriteErrorLine("serviceStartParameters is null", true);
                return null;
            case ETools.ServiceStopper: //სერვისის გამაჩერებელი სერვერის მხარეს
                var serviceStopParameters =
                    ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (serviceStopParameters is not null)
                    return new ServiceStopper(logger, true, serviceStopParameters, parametersManager);
                StShared.WriteErrorLine("serviceStopParameters is null", true);
                return null;
            case ETools.VersionChecker: //სერვისის გამაჩერებელი სერვერის მხარეს
                var checkVersionParameters =
                    CheckVersionParameters.Create(supportToolsParameters, projectName, serverInfo);
                if (checkVersionParameters is not null)
                    return new VersionChecker(logger, true, checkVersionParameters, parametersManager);
                StShared.WriteErrorLine("checkVersionParameters is null", true);
                return null;
            case ETools.RecreateDevDatabase:
            case ETools.DropDevDatabase:
            case ETools.CreateDevDatabaseByMigration:
            case ETools.CorrectNewDatabase:
            case ETools.ScaffoldSeederCreator:
            case ETools.JsonFromProjectDbProjectGetter:
            case ETools.SeedData:
            case ETools.JetBrainsCleanupCode:
            default:
                StShared.WriteErrorLine("Command tool does not created", true, logger);
                return null;
        }
    }
}