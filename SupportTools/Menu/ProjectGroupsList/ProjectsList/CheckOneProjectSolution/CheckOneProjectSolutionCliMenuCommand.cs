using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectSolution;

public sealed class CheckOneProjectSolutionCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check Solution";

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckOneProjectSolutionCliMenuCommand(ILogger logger, IParametersManager parametersManager,
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

        //მხოლოდ ეს ერთი პროექტი მოწმდება და მისი სტატუსი ახლდება. remote პროექტის ფაილები ამ კომპიუტერზე არ არის (D3)
        ProjectSolutionChecker.CheckProjects(parameters,
            RemoteOnlyProjects.Skip([new KeyValuePair<string, ProjectModel>(_projectName, project)], _logger),
            _menuParameters, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
