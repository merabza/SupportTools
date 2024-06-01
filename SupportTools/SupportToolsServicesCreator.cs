using Microsoft.Extensions.DependencyInjection;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools;

public class SupportToolsServicesCreator : ServicesCreator
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServicesCreator(SupportToolsParameters par) : base(par.LogFolder, null, "SupportTools")
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddHttpClient();
    }
}