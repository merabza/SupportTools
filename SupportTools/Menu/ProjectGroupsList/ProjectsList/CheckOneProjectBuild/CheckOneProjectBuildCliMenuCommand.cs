using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectBuild;

public sealed class CheckOneProjectBuildCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check project build";

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckOneProjectBuildCliMenuCommand(ILogger logger, ParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters, string projectName) : base(MenuCommandName, EMenuAction.Reload,
        EMenuAction.Reload, projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
        _projectName = projectName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return ValueTask.FromResult(false);
        }

        //მხოლოდ ეს ერთი პროექტი მოწმდება და მისი სტატუსი ახლდება
        ProjectBuildChecker.CheckProjects([new KeyValuePair<string, ProjectModel>(_projectName, project)],
            _menuParameters, _logger, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
