using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.OutdatedPackages;

namespace SupportTools.Tests.OutdatedPackages;

//records the steps as text ("sync all: <message>", "sync <project>: <message>", "update <project>/<git>"); a step in
//FailingSteps answers false; OnStep runs before a step answers, so a test can move the clock or cancel there
internal sealed class FakeOutdatedPackagesUpdateSteps : IOutdatedPackagesUpdateSteps
{
    public List<string> Steps { get; } = [];
    public HashSet<string> FailingSteps { get; } = [];
    public List<bool> IncludeRemoteProjectsValues { get; } = [];
    public Action<string>? OnStep { get; set; }

    public Task<bool> SyncGits(string? projectName, bool includeRemoteProjects, string commitMessage,
        CancellationToken cancellationToken)
    {
        IncludeRemoteProjectsValues.Add(includeRemoteProjects);
        return Task.FromResult(Answer($"sync {projectName ?? "all"}: {commitMessage}"));
    }

    public bool UpdatePackages(string projectName, string gitName)
    {
        return Answer($"update {projectName}/{gitName}");
    }

    private bool Answer(string step)
    {
        Steps.Add(step);
        OnStep?.Invoke(step);
        return !FailingSteps.Contains(step);
    }
}
