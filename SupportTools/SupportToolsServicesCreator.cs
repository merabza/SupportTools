using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using LibAppProjectCreator.ToolCommands.JetBrainsCleanupCode;
using LibCodeGenerator.ToolCommands.GenerateApiRoutes;
using LibDatabaseWork.ToolCommands.CorrectNewDatabase;
using LibScaffoldSeeder.ToolCommands.ExternalScaffoldSeed.JsonFromProjectDbProjectGetter;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters.DependencyInjection;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools;

public sealed class SupportToolsServicesCreator : ServicesCreator
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServicesCreator(SupportToolsParameters par) : base(par.LogFolder, null, "SupportTools")
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddHttpClient();
        services.AddMemoryCache();

        services.AddSingleton<IApplication, SupportToolsApplication>();

        services.AddTransientAllToolCommandFactoryStrategies(
            typeof(CorrectNewDatabaseToolCommandFactoryStrategy).Assembly,
            typeof(JetBrainsCleanupCodeRunnerToolCommandFactoryStrategy).Assembly,
            typeof(JsonFromProjectDbProjectGetterFactoryStrategy).Assembly,
            typeof(GenerateApiRoutesToolCommandFactoryStrategy).Assembly,
            typeof(ApplicationSettingsEncoderToolCommandFactoryStrategy).Assembly);
    }
}
