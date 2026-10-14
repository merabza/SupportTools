using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

public sealed class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3 : CliMenuCommand
{
    public const string MenuCommandName = "Sync One Project All Gits With Scaffold Seeders V3";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3(ILogger logger,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager, string projectName,
        bool askRunAction = true) : base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, askRunAction)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    //ერთი პროექტის ბრძანება V2-ის მსგავსად მის git-ებს ყოველთვის კლონავს, remote პროექტისასაც
    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var syncMultipleProjectsGitsToolAction = SyncMultipleProjectsGitsToolActionV3.Create(_logger,
            _httpClientFactory, _parametersManager, null, _projectName, true);
        return await syncMultipleProjectsGitsToolAction.Run(cancellationToken);
    }
}
