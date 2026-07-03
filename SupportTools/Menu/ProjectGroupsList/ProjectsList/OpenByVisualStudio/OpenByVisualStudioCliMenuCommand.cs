using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

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

        string? devenvPath = VisualStudioOpener.DetectDevEnvPath(_logger);

        if (string.IsNullOrWhiteSpace(devenvPath))
        {
            StShared.WriteErrorLine("Could not find Visual Studio devenv.exe", true);
            return new ValueTask<bool>(false);
        }

        VisualStudioOpener.OpenSolution(devenvPath, project.SolutionFileName, _logger);

        return new ValueTask<bool>(true);
    }
}
