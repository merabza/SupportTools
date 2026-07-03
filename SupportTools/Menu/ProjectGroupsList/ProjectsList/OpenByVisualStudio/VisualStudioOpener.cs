using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;

public static class VisualStudioOpener
{
    public static void OpenSolution(string devenvPath, string slnFilePath, ILogger? logger)
    {
        StShared.RunProcess(false, logger, devenvPath, slnFilePath, null, true, 0);
    }

    public static string? DetectDevEnvPath(ILogger? logger)
    {
        // Try using vswhere.exe to find the latest Visual Studio installation
        string vswherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (File.Exists(vswherePath))
        {
            OneOf<(string, int), Error[]> runProcessWithOutputResult = StShared.RunProcessWithOutput(true, logger,
                vswherePath, "-latest -products * -requires Microsoft.Component.MSBuild -property installationPath");

            if (runProcessWithOutputResult.IsT0)
            {
                string installPath = runProcessWithOutputResult.AsT0.Item1.RemoveNotNeedLastPart("\r\n");
                if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(installPath))
                {
                    string devenvPath = Path.Combine(installPath, "Common7", "IDE", "devenv.exe");
                    if (File.Exists(devenvPath))
                    {
                        return devenvPath;
                    }
                }
            }
        }

        // Fallback: Search common Visual Studio installation paths
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string vsBasePath = Path.Combine(programFiles, "Microsoft Visual Studio");

        if (!Directory.Exists(vsBasePath))
        {
            return null;
        }

        string[] editions = ["Community", "Professional", "Enterprise"];

        return (from yearDir in Directory.GetDirectories(vsBasePath).OrderByDescending(d => d)
            from edition in editions
            select Path.Combine(yearDir, edition, "Common7", "IDE", "devenv.exe")).FirstOrDefault(File.Exists);
    }
}
