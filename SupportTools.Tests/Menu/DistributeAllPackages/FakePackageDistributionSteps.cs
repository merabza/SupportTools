using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportTools.Menu;
using SupportTools.Menu.DistributeAllPackages;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Tests.Menu.DistributeAllPackages;

//records every step; a step named FailingStep fails for FailingProject (or for every project when it is null).
//The first sync of a project is "sync", the second one "final sync"; a distribution is "distribute" for the project
//that takes the packages; "check" asks whether a package has changed since its last upload, which only the projects
//in UnchangedVersions have not. Shared by the tests of the main menu, group and project commands
internal sealed class FakePackageDistributionSteps : IPackageDistributionSteps
{
    private readonly HashSet<string> _syncedProjectNames = [];

    public List<string> Calls { get; } = [];
    public List<string> CommitMessages { get; } = [];
    public string? FailingStep { get; set; }
    public string? FailingProject { get; set; }
    public ProjectBuildCheckResult? BuildResult { get; set; } = new(EProjectBuildCheckStatus.Success);
    public Dictionary<string, string> UnchangedVersions { get; } = [];

    public Task<bool> SyncProjectGits(string projectName, string commitMessage, CancellationToken cancellationToken)
    {
        CommitMessages.Add(commitMessage);
        string step = _syncedProjectNames.Add(projectName) ? "sync" : "final sync";
        return Task.FromResult(Run(step, projectName));
    }

    public Task<string?> GetUnchangedUploadedPackageVersion(string projectName, CancellationToken cancellationToken)
    {
        Calls.Add($"check {projectName}");
        return Task.FromResult(UnchangedVersions.GetValueOrDefault(projectName));
    }

    public ProjectBuildCheckResult? CheckBuild(string projectName, ProjectModel project, bool noIncremental,
        CancellationToken cancellationToken)
    {
        Calls.Add($"build {projectName} noIncremental={noIncremental}");
        return Fails("build", projectName)
            ? new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 2)
            : BuildResult;
    }

    public bool CheckPackageSolution(string projectName)
    {
        return Run("solution", projectName);
    }

    public bool BuildAndUploadPackage(string projectName)
    {
        return Run("package", projectName);
    }

    public Task<bool> DistributePackagesToProject(string projectName, IReadOnlyList<string> packageProjectNames,
        CancellationToken cancellationToken)
    {
        Calls.Add($"distribute {string.Join(", ", packageProjectNames)} to {projectName}");
        return Task.FromResult(!Fails("distribute", projectName));
    }

    //the calls of all steps of one package that is built and uploaded, in their order
    public static List<string> GetBuiltPackageCalls(string projectName)
    {
        return
        [
            $"sync {projectName}", $"check {projectName}", $"build {projectName} noIncremental=False",
            $"solution {projectName}", $"package {projectName}", $"final sync {projectName}"
        ];
    }

    private bool Run(string step, string projectName)
    {
        Calls.Add($"{step} {projectName}");
        return !Fails(step, projectName);
    }

    private bool Fails(string step, string projectName)
    {
        return FailingStep == step && (FailingProject is null || FailingProject == projectName);
    }
}
