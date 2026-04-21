using AppCliTools.CliTools.Models;
using Microsoft.Extensions.Options;
using SystemTools.SystemToolsShared;

namespace SupportTools;

public class SupportToolsApplication : IApplication
{
    public SupportToolsApplication(IOptions<ApplicationOptions> options)
    {
        AppName = options.Value.AppName;
    }

    public string AppName { get; }
}
