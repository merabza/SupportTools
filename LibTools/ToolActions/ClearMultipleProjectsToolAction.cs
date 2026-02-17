using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibGitWork;
using LibTools.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using ToolsManagement.LibToolActions;

namespace LibTools.ToolActions;

public sealed class ClearMultipleProjectsToolAction : ToolAction
{
    private readonly ClearAllProjectsParameters _clearAllProjectsParameters;
    private readonly string? _excludeFolder;
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;

    private ClearMultipleProjectsToolAction(ILogger logger, ParametersManager parametersManager,
        ClearAllProjectsParameters clearAllProjectsParameters, string? excludeFolder, bool useConsole) : base(logger,
        "Clear Multiple Projects", null, null, useConsole)
    {
        _logger = logger;
        _clearAllProjectsParameters = clearAllProjectsParameters;
        _excludeFolder = excludeFolder;
        _parametersManager = parametersManager;
    }

    public static ClearMultipleProjectsToolAction Create(ILogger logger, ParametersManager parametersManager,
        string? projectGroupName, string? projectName, bool useConsole)
    {
        //D:\1WorkDotnet\SupportTools\SupportTools\SupportTools\bin\Debug\net9.0
        string baseFolder = AppContext.BaseDirectory;
        string? excludeFolder = null;
        var baseDir = new DirectoryInfo(baseFolder);
        if (baseDir.Parent is { Name: "Debug", Parent.Name: "bin" })
        {
            excludeFolder = baseDir.Parent.Parent.Parent?.FullName;
        }

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var clearAllProjectsParameters =
            ClearAllProjectsParameters.Create(supportToolsParameters, projectGroupName, projectName);
        return new ClearMultipleProjectsToolAction(logger, parametersManager, clearAllProjectsParameters, excludeFolder,
            useConsole);
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList = GetProjectsList();

        List<KeyValuePair<string, ProjectModel>> projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        //var changedGitProjects = new Dictionary<EGitCollect, Dictionary<string, List<string>>>
        //{
        //    [EGitCollect.Collect] = [],
        //    [EGitCollect.Usage] = []
        //};
        //var loopNom = 0;
        //var gitCollectUsage = EGitCollect.Collect;
        //while (gitCollectUsage == EGitCollect.Collect || changedGitProjects[EGitCollect.Collect].Count > 0)
        //{
        //changedGitProjects[EGitCollect.Collect] = [];
        //Console.WriteLine($"---=== {gitCollectUsage} {(loopNom == 0 ? string.Empty : loopNom)} ===---");
        //პროექტების ჩამონათვალი
        foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        {
            ClearOneSolution(projectName, project, EGitCol.Main);
            if (_clearAllProjectsParameters.ScaffoldSeedersWorkFolder is not null)
            {
                ClearOneSolution(projectName, project, EGitCol.ScaffoldSeed);
            }
        }

        //Console.WriteLine("---===---------===---");

        //gitCollectUsage = EGitCollect.Usage;
        //loopNom++;
        //changedGitProjects[EGitCollect.Usage] = changedGitProjects[EGitCollect.Collect];
        //}

        return ValueTask.FromResult(true);
    }

    private IEnumerable<KeyValuePair<string, ProjectModel>> GetProjectsList()
    {
        if (_clearAllProjectsParameters.ProjectGroupName is null && _clearAllProjectsParameters.ProjectName is null)
        {
            return _clearAllProjectsParameters.Projects;
        }

        if (_clearAllProjectsParameters.ProjectGroupName is not null)
        {
            return _clearAllProjectsParameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _clearAllProjectsParameters.ProjectGroupName);
        }

        return _clearAllProjectsParameters.Projects.Where(x => x.Key == _clearAllProjectsParameters.ProjectName);
    }

    private void ClearOneSolution(string projectName, ProjectModel project, EGitCol gitCol)
    {
        if (!GitStat.CheckGitProject(projectName, project, gitCol, false))
        {
            return;
        }

        var clearOneProjectAllGitsToolAction =
            ClearOneProjectAllGitsToolAction.Create(_logger, _parametersManager, projectName, gitCol, _excludeFolder);
        clearOneProjectAllGitsToolAction?.Run(CancellationToken.None).Wait();
    }
}
