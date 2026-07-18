using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;

public sealed class CheckPackageSolutionCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check Package Solution";

    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckPackageSolutionCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName) :
        base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var packageSolutionChecker = new PackageSolutionChecker(_logger, _parametersManager, _projectName);
        return ValueTask.FromResult(packageSolutionChecker.CheckPackageSolution());
    }
}
