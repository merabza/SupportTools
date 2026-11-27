using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.CheckAllProjectsSolutions;

public sealed class CheckAllProjectsSolutionsCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check Solutions";

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    //შემოწმება მხოლოდ ფაილებს კითხულობს და სწრაფია, ამიტომ გაშვებამდე არაფერს ეკითხება
    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckAllProjectsSolutionsCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base(MenuCommandName, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ყველა პროექტი თავიდან მოწმდება, ამიტომ წინა შედეგებს ვასუფთავებთ
        _menuParameters.ProjectSolutionCheckResults.Clear();
        //remote პროექტების ფაილები ამ კომპიუტერზე არ არის, ამიტომ ისინი არ მოწმდება (D3)
        ProjectSolutionChecker.CheckProjects(parameters, RemoteOnlyProjects.Skip(parameters.Projects, _logger),
            _menuParameters, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
