using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

public sealed class PackageDistributionCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Package distribution";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PackageDistributionCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string projectName) : base(MenuCommandName, EMenuAction.Reload,
        EMenuAction.Reload, projectName, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            $"This process will replace ProjectReferences of {_projectName} with PackageReferences in main repositories of all consumer projects");
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var packageDistributor = new PackageDistributor(_logger, _httpClientFactory, _parametersManager, _projectName);
        return await packageDistributor.DistributePackage(cancellationToken);
    }
}
