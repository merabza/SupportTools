using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;

public sealed class OpenByVisualStudioCliMenuCommand : CliMenuCommand
{
    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public OpenByVisualStudioCliMenuCommand(IParametersManager parametersManager, string projectName, ILogger? logger) :
        base("Open By Visual Studio", EMenuAction.Reload, EMenuAction.Reload, projectName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _logger = logger;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return new ValueTask<bool>(false);
        }

        if (string.IsNullOrWhiteSpace(project.SolutionFileName))
        {
            StShared.WriteErrorLine($"Project {_projectName} does not have a solution file", true);
            return new ValueTask<bool>(false);
        }

        if (!File.Exists(project.SolutionFileName))
        {
            StShared.WriteErrorLine($"Project {_projectName} solution file {project.SolutionFileName} does not exists",
                true);
            return new ValueTask<bool>(false);
        }

        OpenSolution(project.SolutionFileName);

        return new ValueTask<bool>(true);
    }

    private void OpenSolution(string slnFilePath)
    {
        string? devenvPath = DetectDevEnvPath();

        if (string.IsNullOrWhiteSpace(devenvPath))
        {
            StShared.WriteErrorLine("Could not find Visual Studio devenv.exe", true);
            return;
        }

        StShared.RunProcess(false, _logger, devenvPath, slnFilePath, null, true, 0);
    }

    private string? DetectDevEnvPath()
    {
        // Try using vswhere.exe to find the latest Visual Studio installation
        string vswherePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (File.Exists(vswherePath))
        {
            OneOf<(string, int), Error[]> runProcessWithOutputResult = StShared.RunProcessWithOutput(true, _logger,
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
