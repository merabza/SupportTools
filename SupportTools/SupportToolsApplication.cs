using AppCliTools.CliTools.Models;
using Microsoft.Extensions.Options;
using SystemTools.SystemToolsShared;

namespace SupportTools;

public class SupportToolsApplication : IApplication
{
    private readonly string _appName;

    public SupportToolsApplication(IOptions<ApplicationOptions> options)
    {
        _appName = options.Value.AppName;
    }

    public string AppName => _appName;
}
