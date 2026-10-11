using System;
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
    private readonly Func<bool> _inputNoIncremental;

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    public CheckAllProjectsBuildCliMenuCommand(ILogger logger, string appName, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : this(logger, appName, parametersManager, menuParameters,
        ProjectBuildChecker.InputNoIncremental)
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal CheckAllProjectsBuildCliMenuCommand(ILogger logger, string appName, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters, Func<bool> inputNoIncremental) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _appName = appName;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
        _inputNoIncremental = inputNoIncremental;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        bool noIncremental = _inputNoIncremental();

        //ყველა პროექტი თავიდან მოწმდება, ამიტომ წინა შედეგებს ვასუფთავებთ
        _menuParameters.ProjectBuildCheckResults.Clear();
        //remote პროექტების solution ფაილი ამ კომპიუტერზე არ არის, ამიტომ ისინი არ მოწმდება (D3)
        ProjectBuildChecker.CheckProjects(_appName, RemoteOnlyProjects.Skip(parameters.Projects, _logger),
            _menuParameters, _logger, noIncremental, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
