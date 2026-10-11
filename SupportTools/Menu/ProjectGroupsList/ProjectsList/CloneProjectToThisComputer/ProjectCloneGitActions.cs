using LibGitData;
using LibGitWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//git-ის რეალური ნაბიჯები: სკანირება როგორც Update Git Projects-ში (GitProjectsUpdater, SupportTools-ის WorkFolder\Gits-ში),
//კლონი როგორც Sync ... Gits V2-ში (GitProjectSyncronizer)
internal sealed class ProjectCloneGitActions : IProjectCloneGitActions
{
    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectCloneGitActions(ILogger logger, IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        _logger = parameters.LogGitWork ? logger : null;
        _parametersManager = parametersManager;
    }

    public bool ScanGitProjects(string gitName)
    {
        GitProjectsUpdater? gitProjectsUpdater = GitProjectsUpdater.Create(_logger, _parametersManager, gitName, true);
        return gitProjectsUpdater?.ProcessOneGitProject() is not null;
    }

    public void SyncGit(string projectName, EGitCol gitCol, string gitName)
    {
        var gitProjectSyncronizer = new GitProjectSyncronizer(_logger, _parametersManager, gitName, true);
        gitProjectSyncronizer.Add(projectName, gitCol);
        if (gitProjectSyncronizer.Count > 0)
        {
            gitProjectSyncronizer.RunSync();
        }
    }
}
