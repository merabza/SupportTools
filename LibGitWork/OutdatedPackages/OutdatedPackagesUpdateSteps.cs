using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibGitWork.OutdatedPackages;

public sealed class OutdatedPackagesUpdateSteps : IOutdatedPackagesUpdateSteps
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public OutdatedPackagesUpdateSteps(ILogger? logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    //V3: ცვლილებების გარეშე, remote-ის ცნობილ ვერსიაზე მდგარი ფოლდერი git სერვერს არ მიმართავს, ამიტომ განახლების წინა და
    //შემდეგი სინქრონიზაცია მხოლოდ იმ ფოლდერებზე მუშაობს, რომლებიც წინა git-ის push-მა მოაძველა ან განახლებამ შეცვალა
    public Task<bool> SyncGits(string? projectName, bool includeRemoteProjects, string commitMessage,
        CancellationToken cancellationToken)
    {
        var syncMultipleProjectsGitsToolAction = SyncMultipleProjectsGitsToolActionV3.Create(_logger,
            _httpClientFactory, _parametersManager, null, projectName, includeRemoteProjects, commitMessage);
        return syncMultipleProjectsGitsToolAction.Run(cancellationToken);
    }

    public bool UpdatePackages(string projectName, string gitName)
    {
        var packageUpdaterToolAction =
            PackageUpdaterToolAction.Create(_logger, _parametersManager, projectName, gitName, true);
        return packageUpdaterToolAction is not null && packageUpdaterToolAction.RunPackageUpdate();
    }
}
