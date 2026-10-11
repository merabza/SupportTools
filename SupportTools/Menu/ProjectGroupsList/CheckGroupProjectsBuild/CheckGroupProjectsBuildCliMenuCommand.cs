using System;
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
    private readonly Func<bool> _inputNoIncremental;

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;

    public CheckGroupProjectsBuildCliMenuCommand(ILogger logger, string appName, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters, string projectGroupName) : this(logger, appName, parametersManager,
        menuParameters, projectGroupName, ProjectBuildChecker.InputNoIncremental)
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal CheckGroupProjectsBuildCliMenuCommand(ILogger logger, string appName,
        IParametersManager parametersManager, SupportToolsMenuParameters menuParameters, string projectGroupName,
        Func<bool> inputNoIncremental) : base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _appName = appName;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
        _projectGroupName = projectGroupName;
        _inputNoIncremental = inputNoIncremental;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        bool noIncremental = _inputNoIncremental();

        //მხოლოდ ამ ჯგუფის პროექტები მოწმდება, დანარჩენი ჯგუფების სტატუსები ხელუხლებელი რჩება
        var groupProjects = parameters.Projects.Where(x =>
            SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName);
        //remote პროექტების solution ფაილი ამ კომპიუტერზე არ არის, ამიტომ ისინი არ მოწმდება (D3)
        ProjectBuildChecker.CheckProjects(_appName, RemoteOnlyProjects.Skip(groupProjects, _logger), _menuParameters,
            _logger, noIncremental, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
