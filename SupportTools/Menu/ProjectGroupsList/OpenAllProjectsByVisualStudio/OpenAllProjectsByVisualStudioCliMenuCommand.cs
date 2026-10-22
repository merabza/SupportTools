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

    private readonly Func<CancellationToken, Task> _delayBetweenOpens;
    private readonly Func<ILogger?, string?> _detectDevEnvPath;
    private readonly ILogger? _logger;
    private readonly Action<string, string, ILogger?> _openSolution;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;

    public OpenAllProjectsByVisualStudioCliMenuCommand(IParametersManager parametersManager, string projectGroupName,
        ILogger? logger) : this(parametersManager, projectGroupName, logger, VisualStudioOpener.DetectDevEnvPath,
        VisualStudioOpener.OpenSolution,
        cancellationToken => Task.Delay(DelayBetweenOpensMilliseconds, cancellationToken))
    {
    }

    //Visual Studio-ს მოძებნა, გახსნა და დაყოვნება პარამეტრებადაა გამოტანილი, რომ ტესტებმა devenv არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal OpenAllProjectsByVisualStudioCliMenuCommand(IParametersManager parametersManager, string projectGroupName,
        ILogger? logger, Func<ILogger?, string?> detectDevEnvPath, Action<string, string, ILogger?> openSolution,
        Func<CancellationToken, Task> delayBetweenOpens) : base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload,
        null, true)
    {
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
        _logger = logger;
        _detectDevEnvPath = detectDevEnvPath;
        _openSolution = openSolution;
        _delayBetweenOpens = delayBetweenOpens;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მხოლოდ ამ ჯგუფის ამ კომპიუტერზე არსებული პროექტები იხსნება (D3), სახელების ანბანური თანმიმდევრობით
        List<KeyValuePair<string, ProjectModel>> groupProjects =
        [
            .. RemoteOnlyProjects
                .Skip(
                    parameters.Projects.Where(x =>
                        SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName),
                    _logger).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
        ];

        string? devenvPath = _detectDevEnvPath(_logger);

        if (string.IsNullOrWhiteSpace(devenvPath))
        {
            StShared.WriteErrorLine("Could not find Visual Studio devenv.exe", true);
            return false;
        }

        bool isFirst = true;

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
                await _delayBetweenOpens(cancellationToken);
            }

            Console.WriteLine($"Opening {projectName} solution {project.SolutionFileName}...");
            _openSolution(devenvPath, project.SolutionFileName, _logger);
            isFirst = false;
        }

        return true;
    }
}
