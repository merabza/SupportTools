using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsBuild;

public sealed class CheckGroupProjectsBuildCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check group projects build";
    private readonly string _appName;

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckGroupProjectsBuildCliMenuCommand(ILogger logger, string appName, ParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters, string projectGroupName) : base(MenuCommandName, EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _appName = appName;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
        _projectGroupName = projectGroupName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მხოლოდ ამ ჯგუფის პროექტები მოწმდება, დანარჩენი ჯგუფების სტატუსები ხელუხლებელი რჩება
        var groupProjects = parameters.Projects.Where(x =>
            SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName);
        ProjectBuildChecker.CheckProjects(_appName, groupProjects, _menuParameters, _logger, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
