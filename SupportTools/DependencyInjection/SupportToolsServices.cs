using AppCliTools.CliMenu;
using AppCliTools.CliTools.DependencyInjection;
using AppCliTools.CliTools.Menu.RecentCommandsList;
using AppCliTools.CliTools.Services.MenuBuilder;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using LibAppProjectCreator.ToolCommands.JetBrainsCleanupCode;
using LibCodeGenerator.ToolCommands.GenerateApiRoutes;
using LibDatabaseWork.ToolCommands.CorrectNewDatabase;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using Serilog.Events;
using SupportTools.AutoSync;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SerilogStuff.DependencyInjection;
using SystemTools.SystemToolsShared.DependencyInjection;

namespace SupportTools.DependencyInjection;

public static class SupportToolsServices
{
    public static IServiceCollection AddServices(this IServiceCollection services, string appName,
        SupportToolsParameters par, string parametersFileName)
    {
        // @formatter:off
        services
            .AddSerilogLoggerService(LogEventLevel.Information, appName, par.LogFolder)
            .AddHttpClient()
            .AddMemoryCache()
            .AddSingleton<SupportToolsMenuParameters>()
            .AddTransientAllStrategies<IMenuCommandListFactoryStrategy>(
                typeof(ProjectGroupsListFactoryStrategy).Assembly, 
                typeof(RecentCommandsListFactoryStrategy).Assembly)
            .AddSingleton<IProcesses, Processes>()
            .AddSingleton<IMenuBuilder, SupportToolsMenuBuilder>()
            .AddTransientAllStrategies<IMenuCommandFactoryStrategy>(
                typeof(SupportToolsParametersEditorListCliMenuCommandFactoryStrategy).Assembly)
            .AddTransientAllStrategies<IToolCommandFactoryStrategy>(
                typeof(CorrectNewDatabaseToolCommandFactoryStrategy).Assembly,
                typeof(JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy).Assembly,
                typeof(GenerateApiRoutesToolCommandFactoryStrategy).Assembly,
                typeof(ApplicationSettingsEncoderToolCommandFactoryStrategy).Assembly)
            .AddApp(x =>
            {
                x.AppName = appName;
            })
            //IParametersManager ავტომატური სინქრონიზაციის decorator-ია (D2), რომელიც
            //SupportToolsMainParametersManager-ს ფუთავს. AddMainParametersManager<T> T : ParametersManager-ს მოითხოვს
            //და decorator-ს ვერ დაარეგისტრირებს, ამიტომ მისი options და singleton-ები აქ ხელითაა
            .Configure<MainParametersManagerOptions>(x =>
            {
                x.ParametersFileName = parametersFileName;
                x.Par = par;
            })
            .AddSingleton<SupportToolsMainParametersManager>()
            .AddSingleton<RegistryAutoSync>()
            .AddSingleton<IParametersManager, AutoSyncParametersManager>();

        // @formatter:on
        services.AddRecentCommandsService(x =>
        {
            x.RecentCommandsFileName = par.RecentCommandsFileName;
            x.RecentCommandsCount = par.RecentCommandsCount;
        });

        return services;
    }
}
