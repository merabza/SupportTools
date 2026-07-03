using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.OpenAllProjectsByVisualStudio;

public sealed class OpenAllProjectsByVisualStudioCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Open all projects By Visual Studio";

    //დაყოვნება გახსნებს შორის, რომ არ გაიჭედოს გახსნის პროცესი
    private const int DelayBetweenOpensMilliseconds = 5000;

    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public OpenAllProjectsByVisualStudioCliMenuCommand(IParametersManager parametersManager, string projectGroupName,
        ILogger? logger) : base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
        _logger = logger;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მხოლოდ ამ ჯგუფის პროექტები იხსნება, სახელების ანბანური თანმიმდევრობით
        List<KeyValuePair<string, ProjectModel>> groupProjects = parameters.Projects
            .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();

        string? devenvPath = VisualStudioOpener.DetectDevEnvPath(_logger);

        if (string.IsNullOrWhiteSpace(devenvPath))
        {
            StShared.WriteErrorLine("Could not find Visual Studio devenv.exe", true);
            return false;
        }

        var isFirst = true;

        foreach (KeyValuePair<string, ProjectModel> kvp in groupProjects)
        {
            string projectName = kvp.Key;
            ProjectModel project = kvp.Value;

            if (string.IsNullOrWhiteSpace(project.SolutionFileName))
            {
                StShared.WriteErrorLine($"Project {projectName} does not have a solution file", true);
                continue;
            }

            if (!File.Exists(project.SolutionFileName))
            {
                StShared.WriteErrorLine(
                    $"Project {projectName} solution file {project.SolutionFileName} does not exists", true);
                continue;
            }

            if (!isFirst)
            {
                await Task.Delay(DelayBetweenOpensMilliseconds, cancellationToken);
            }

            Console.WriteLine($"Opening {projectName} solution {project.SolutionFileName}...");
            VisualStudioOpener.OpenSolution(devenvPath, project.SolutionFileName, _logger);
            isFirst = false;
        }

        return true;
    }
}
