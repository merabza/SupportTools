using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;

public sealed class BuildPackageCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Build Package";

    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BuildPackageCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName) : base(
        MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var packageBuilder = new PackageBuilder(_logger, _parametersManager, _projectName);
        return ValueTask.FromResult(packageBuilder.BuildAndUploadPackage());
    }
}
