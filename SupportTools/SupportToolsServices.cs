using System;
using AppCliTools.CliMenu;
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
using ParametersManagement.LibParameters.DependencyInjection;
using SupportTools.Menu.ProjectGroupsList;
using SupportTools.Menu.SupportToolsParametersEdit;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace SupportTools;

public static class SupportToolsServices
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddHttpClient().AddMemoryCache();

        services.AddTransientAllMenuCommandsListFactoryStrategies(typeof(ProjectGroupsListFactoryStrategy).Assembly);

        services.AddSingleton<IProcesses, Processes>();
        services.AddSingleton<IMenuBuilder, SupportToolsMenuBuilder>();

        return services;
    }

    public static IServiceCollection AddToolCommandsFactoryStrategies(this IServiceCollection services)
    {
        services.AddTransientAllToolCommandFactoryStrategies(
            typeof(CorrectNewDatabaseToolCommandFactoryStrategy).Assembly,
            typeof(JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy).Assembly,
            typeof(JsonFromProjectDbProjectGetterFactoryStrategy).Assembly,
            typeof(GenerateApiRoutesToolCommandFactoryStrategy).Assembly,
            typeof(ApplicationSettingsEncoderToolCommandFactoryStrategy).Assembly);
        return services;
    }

    public static IServiceCollection AddMenuCommandsFactoryStrategies(this IServiceCollection services)
    {
        services.AddTransientAllMenuCommandFactoryStrategies(
            typeof(SupportToolsParametersEditorListCliMenuCommandFactoryStrategy).Assembly);
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services,
        Action<ApplicationOptions> setupAction)
    {
        services.AddSingleton<IApplication, SupportToolsApplication>();
        services.Configure(setupAction);
        return services;
    }

    public static IServiceCollection AddMainParametersManager(this IServiceCollection services,
        Action<MainParametersManagerOptions> setupAction)
    {
        services.AddSingleton<IParametersManager, ParametersManager>();
        services.Configure(setupAction);
        return services;
    }

    public static IServiceCollection AddRecentCommandsService(this IServiceCollection services,
        Action<RecentCommandOptions> setupAction)
    {
        services.AddSingleton<IRecentCommandsService, RecentCommandsService>();
        services.Configure(setupAction);
        return services;
    }
}
