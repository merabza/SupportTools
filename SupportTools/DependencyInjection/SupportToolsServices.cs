using AppCliTools.CliMenu;
using AppCliTools.CliMenu.DependencyInjection;
using AppCliTools.CliTools.DependencyInjection;
using AppCliTools.CliTools.Menu.RecentCommandsList;
using AppCliTools.CliTools.Services.MenuBuilder;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using LibAppProjectCreator.ToolCommands.JetBrainsCleanupCode;
using LibCodeGenerator.ToolCommands.GenerateApiRoutes;
using LibDatabaseWork.ToolCommands.CorrectNewDatabase;
using LibScaffoldSeeder.ToolCommands.ExternalScaffoldSeed.JsonFromProjectDbProjectGetter;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using ParametersManagement.LibParameters.DependencyInjection;
using Serilog.Events;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;

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
                typeof(JsonFromProjectDbProjectGetterFactoryStrategy).Assembly,
                typeof(GenerateApiRoutesToolCommandFactoryStrategy).Assembly,
                typeof(ApplicationSettingsEncoderToolCommandFactoryStrategy).Assembly)
            .AddApplication(x =>
            {
                x.AppName = appName;
            })
            .AddMainParametersManager(x =>
            {
                x.ParametersFileName = parametersFileName;
                x.Par = par;
            });

        // @formatter:on
        services.AddRecentCommandsService(x =>
        {
            x.RecentCommandsFileName = par.RecentCommandsFileName;
            x.RecentCommandsCount = par.RecentCommandsCount;
        });

        return services;
    }
}
