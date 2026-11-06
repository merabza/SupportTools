using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork;
using LibGitWork.OutdatedPackages;

namespace SupportTools.Tests.OutdatedPackages;

//records the steps as text: "sync all: <message>", "sync <project>: <message>", and one "update <project>/<git>, …" for
//the gits that are updated together. A sync in FailingSteps answers false; an update "update <project>/<git>" in
//FailingSteps answers OutdatedFailed for that git, one in RestoreFailingSteps RestoreAfterUpdateFailed. OnStep runs
//before a step answers, so a test can move the clock or cancel there
internal sealed class FakeOutdatedPackagesUpdateSteps : IOutdatedPackagesUpdateSteps
{
    public List<string> Steps { get; } = [];
    public HashSet<string> FailingSteps { get; } = [];
    public HashSet<string> RestoreFailingSteps { get; } = [];
    public List<bool> IncludeRemoteProjectsValues { get; } = [];
    public Action<string>? OnStep { get; set; }

    public Task<bool> SyncGits(string? projectName, bool includeRemoteProjects, string commitMessage,
        CancellationToken cancellationToken)
    {
        IncludeRemoteProjectsValues.Add(includeRemoteProjects);
        string step = $"sync {projectName ?? "all"}: {commitMessage}";
        Record(step);
        return Task.FromResult(!FailingSteps.Contains(step));
    }

    public IReadOnlyList<EPackageUpdateResult> UpdatePackages(IReadOnlyList<OutdatedPackagesUpdateStep> steps,
        CancellationToken cancellationToken)
    {
        Record($"update {string.Join(", ", steps.Select(x => $"{x.ProjectName}/{x.GitName}"))}");
        return [.. steps.Select(x => Answer($"update {x.ProjectName}/{x.GitName}"))];
    }

    private EPackageUpdateResult Answer(string update)
    {
        if (FailingSteps.Contains(update))
        {
            return EPackageUpdateResult.OutdatedFailed;
        }

        return RestoreFailingSteps.Contains(update)
            ? EPackageUpdateResult.RestoreAfterUpdateFailed
            : EPackageUpdateResult.Success;
    }

    private void Record(string step)
    {
        Steps.Add(step);
        OnStep?.Invoke(step);
    }
}
