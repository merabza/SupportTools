using System;
using AppCliTools.CliMenu;
using AppCliTools.CliMenu.DependencyInjection;
using AppCliTools.CliTools.DependencyInjection;
using AppCliTools.CliTools.Models;
using AppCliTools.CliTools.Services.MenuBuilder;
using AppCliTools.CliTools.Services.RecentCommands;
using AppCliTools.CliTools.Services.RecentCommands.Models;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using LibAppProjectCreator.ToolCommands.JetBrainsCleanupCode;
using LibCodeGenerator.ToolCommands.GenerateApiRoutes;
using LibDatabaseWork.ToolCommands.CorrectNewDatabase;
using LibScaffoldSeeder.ToolCommands.ExternalScaffoldSeed.JsonFromProjectDbProjectGetter;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using Serilog.Events;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

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
            .AddSingleton<MenuParameters>()
            .AddTransientAllStrategies<IMenuCommandListFactoryStrategy>(
                typeof(ProjectGroupsListFactoryStrategy).Assembly)
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
        if (!string.IsNullOrWhiteSpace(par.RecentCommandsFileName) && par.RecentCommandsCount > 0)
        {
            services.AddRecentCommandsService(x =>
            {
                x.RecentCommandsFileName = par.RecentCommandsFileName;
                x.RecentCommandsCount = par.RecentCommandsCount;
            });
        }

        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services,
        Action<ApplicationOptions> setupAction)
    {
        services.AddSingleton<IApplication, SupportToolsApplication>();
        services.Configure(setupAction);
        return services;
    }

    private static IServiceCollection AddMainParametersManager(this IServiceCollection services,
        Action<MainParametersManagerOptions> setupAction)
    {
        services.AddSingleton<IParametersManager, ParametersManager>();
        services.Configure(setupAction);
        return services;
    }

    private static IServiceCollection AddRecentCommandsService(this IServiceCollection services,
        Action<RecentCommandOptions> setupAction)
    {
        services.AddSingleton<IRecentCommandsService, RecentCommandsService>();
        services.Configure(setupAction);
        return services;
    }
}
