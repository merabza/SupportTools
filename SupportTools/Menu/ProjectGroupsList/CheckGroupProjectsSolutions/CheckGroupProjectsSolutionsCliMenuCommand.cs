using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsSolutions;

public sealed class CheckGroupProjectsSolutionsCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Check Solutions";

    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;

    //შემოწმება მხოლოდ ფაილებს კითხულობს და სწრაფია, ამიტომ გაშვებამდე არაფერს ეკითხება
    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckGroupProjectsSolutionsCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters, string projectGroupName) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload)
    {
        _logger = logger;
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
        //remote პროექტების ფაილები ამ კომპიუტერზე არ არის, ამიტომ ისინი არ მოწმდება (D3)
        ProjectSolutionChecker.CheckProjects(parameters, RemoteOnlyProjects.Skip(groupProjects, _logger),
            _menuParameters, cancellationToken);

        return ValueTask.FromResult(true);
    }
}
