using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.CheckAllProjectsBuild;

public sealed class CheckAllProjectsBuildCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check all projects build";
    private readonly string _appName;

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckAllProjectsBuildCliMenuCommand(ILogger logger, string appName, ParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null,
        true)
    {
        _logger = logger;
        _appName = appName;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ყველა პროექტი თავიდან მოწმდება, ამიტომ წინა შედეგებს ვასუფთავებთ
        _menuParameters.ProjectBuildCheckStatuses.Clear();
        ProjectBuildChecker.CheckProjects(_appName, parameters.Projects, _menuParameters, _logger, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
