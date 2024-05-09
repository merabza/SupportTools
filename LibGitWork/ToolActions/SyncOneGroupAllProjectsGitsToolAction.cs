//using System;
//using System.Linq;
//using LibGitWork.ToolCommandParameters;
//using LibParameters;
//using LibToolActions;
//using Microsoft.Extensions.Logging;
//using SupportToolsData.Models;
//using SupportToolsData;
//using System.Threading.Tasks;
//using System.Threading;
//using SystemToolsShared;

//namespace LibGitWork.ToolActions;

//public class SyncOneGroupAllProjectsGitsToolAction : ToolAction
//{
//    private readonly ILogger _logger;
//    private readonly ParametersManager _parametersManager;
//    private readonly SyncOneGroupAllProjectsGitsParameters _syncOneGroupAllProjectsGitsParameters;

//    private SyncOneGroupAllProjectsGitsToolAction(ILogger logger, ParametersManager parametersManager,
//        SyncOneGroupAllProjectsGitsParameters syncOneGroupAllProjectsGitsParameters) : base(logger, "Sync One Group All Projects Gits",
//        null, null)
//    {
//        _logger = logger;
//        _parametersManager = parametersManager;
//        _syncOneGroupAllProjectsGitsParameters = syncOneGroupAllProjectsGitsParameters;
//    }


//    public static SyncOneGroupAllProjectsGitsToolAction Create(ILogger logger, ParametersManager parametersManager, string projectGroupName)
//    {
//        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
//        var syncOneGroupAllProjectsGitsParameters = SyncOneGroupAllProjectsGitsParameters.Create(supportToolsParameters, projectGroupName);

//        return new SyncOneGroupAllProjectsGitsToolAction(logger, parametersManager, syncOneGroupAllProjectsGitsParameters);
//    }

//    protected override Task<bool> RunAction(CancellationToken cancellationToken)
//    {
//        //პროექტების ჩამონათვალი
//        foreach (var kvp in _syncOneGroupAllProjectsGitsParameters.Projects
//                     .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
//                                 _syncOneGroupAllProjectsGitsParameters.ProjectGroupName)
//                     .OrderBy(o => o.Key))
//        {
//            SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.Main);
//            if (_syncOneGroupAllProjectsGitsParameters.ScaffoldSeedersWorkFolder is not null)
//                SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.ScaffoldSeed);
//        }

//        return Task.FromResult(true);
//    }
//    private void SyncAllGitsForOneProject(string projectName, ProjectModel project, EGitCol gitCol)
//    {
//        switch (gitCol)
//        {
//            case EGitCol.Main:
//                if (!string.IsNullOrWhiteSpace(project.ProjectFolderName))
//                    break;
//                StShared.WriteErrorLine($"ProjectFolderName is not specified for project {projectName}", true);
//                return;
//            case EGitCol.ScaffoldSeed:
//                if (!string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
//                    break;
//                StShared.WriteWarningLine($"ScaffoldSeederProjectName is not specified for project {projectName}",
//                    true);
//                return;
//            default:
//                throw new ArgumentOutOfRangeException(nameof(gitCol), gitCol, null);
//        }

//        var syncAllGitsCliMenuCommandMain = SyncOneProjectAllGitsToolAction.Create(_logger, _parametersManager, projectName, gitCol);
//        syncAllGitsCliMenuCommandMain?.Run(CancellationToken.None).Wait();
//    }
//}

