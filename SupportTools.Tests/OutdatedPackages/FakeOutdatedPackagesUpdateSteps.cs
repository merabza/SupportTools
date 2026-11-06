using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork;
using LibGitWork.OutdatedPackages;

namespace SupportTools.Tests.OutdatedPackages;

//records the steps as text ("sync all: <message>", "sync <project>: <message>", "update <project>/<git>"); a step in
//FailingSteps fails (a sync answers false, an update OutdatedFailed), an update in RestoreFailingSteps answers
//RestoreAfterUpdateFailed; OnStep runs before a step answers, so a test can move the clock or cancel there
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
        return Task.FromResult(Answer($"sync {projectName ?? "all"}: {commitMessage}"));
    }

    public EPackageUpdateResult UpdatePackages(string projectName, string gitName)
    {
        string step = $"update {projectName}/{gitName}";
        if (!Answer(step))
        {
            return EPackageUpdateResult.OutdatedFailed;
        }

        return RestoreFailingSteps.Contains(step)
            ? EPackageUpdateResult.RestoreAfterUpdateFailed
            : EPackageUpdateResult.Success;
    }

    private bool Answer(string step)
    {
        Steps.Add(step);
        OnStep?.Invoke(step);
        return !FailingSteps.Contains(step);
    }
}
