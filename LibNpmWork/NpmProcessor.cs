using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibNpmWork;

public sealed class NpmProcessor
{
    private readonly ILogger? _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NpmProcessor(ILogger? logger)
    {
        _logger = logger;
    }

    public bool CreatingReactAppUsingVite(string createInPath, string projectName)
    {
        if (StShared.RunCmdProcess($"npm init --yes vite@latest {projectName} -- --template=react-ts", createInPath))
            return true;
        StShared.WriteErrorLine("Error When creating react app using npm", true, _logger);
        return false;
    }

    public bool InstallNpmPackage(string spaProjectPath, string npmPackageName)
    {
        if (StShared.RunCmdProcess($"npm install {npmPackageName}", spaProjectPath))
            return true;
        StShared.WriteErrorLine("Error When Installing npm package", true, _logger);
        return false;
    }

    public bool InstallNpmPackages(string spaProjectPath)
    {
        if (StShared.RunCmdProcess("npm install", spaProjectPath))
            return true;
        StShared.WriteErrorLine("Error When Installing npm packages", true, _logger);
        return false;
    }
}