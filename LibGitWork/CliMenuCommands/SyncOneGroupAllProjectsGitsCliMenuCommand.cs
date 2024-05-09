using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncOneGroupAllProjectsGitsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneGroupAllProjectsGitsCliMenuCommand(ILogger logger, ParametersManager parametersManager,
        string projectGroupName) : base("Sync One Group All Projects Gits", null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        var syncOneGroupAllProjectsGitsToolAction =
            SyncMultipleProjectsGitsToolAction.Create(_logger, _parametersManager, _projectGroupName);
        syncOneGroupAllProjectsGitsToolAction.Run(CancellationToken.None).Wait();
    }
}

//public sealed class SyncOneGroupAllProjectsGitsCliMenuCommand : CliMenuCommand
//{
//    private readonly ILogger _logger;
//    private readonly ParametersManager _parametersManager;
//    private readonly string _projectGroupName;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public SyncOneGroupAllProjectsGitsCliMenuCommand(ILogger logger, ParametersManager parametersManager,
//        string projectGroupName) : base("Sync Group All Projects Gits", null, true, EStatusView.Brackets, false, true,
//        logger)
//    {
//        _logger = logger;
//        _parametersManager = parametersManager;
//        _projectGroupName = projectGroupName;
//    }

//    protected override void RunAction()
//    {
//        MenuAction = EMenuAction.Reload;
//        var scaffoldSeedersWorkFolderSpecified = true;

//        try
//        {
//            var parameters = (SupportToolsParameters)_parametersManager.Parameters;
//            if (string.IsNullOrWhiteSpace(parameters.ScaffoldSeedersWorkFolder))
//            {
//                StShared.WriteWarningLine("ScaffoldSeedersWorkFolder is not specified", true);
//                scaffoldSeedersWorkFolderSpecified = false;
//            }

//            //პროექტების ჩამონათვალი
//            foreach (var kvp in parameters.Projects
//                         .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
//                                     _projectGroupName)
//                         .OrderBy(o => o.Key))
//            {
//                SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.Main);
//                if (scaffoldSeedersWorkFolderSpecified)
//                    SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.ScaffoldSeed);
//            }
//        }
//        catch (DataInputEscapeException)
//        {
//            Console.WriteLine();
//            Console.WriteLine("Escape... ");
//            StShared.Pause();
//        }
//        catch (Exception e)
//        {
//            StShared.WriteException(e, true);
//        }
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

//        var syncAllGitsCliMenuCommandMain =
//            new SyncOneProjectAllGitsCliMenuCommand(_logger, _parametersManager, projectName, gitCol, false);
//        syncAllGitsCliMenuCommandMain.Run();
//    }
//}