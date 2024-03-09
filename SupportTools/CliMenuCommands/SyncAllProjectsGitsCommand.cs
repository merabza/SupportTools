using CliMenu;
using LibDataInput;
using LibParameters;
using SupportToolsData.Models;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class SyncAllProjectsGitsCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncAllProjectsGitsCommand(ILogger logger, ParametersManager parametersManager) : base(
        "Sync All Projects Gits", null, true, EStatusView.Brackets, false, true, logger)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;
        var scaffoldSeedersWorkFolderSpecified = true;

        try
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;
            if (string.IsNullOrWhiteSpace(parameters.ScaffoldSeedersWorkFolder))
            {
                StShared.WriteWarningLine("ScaffoldSeedersWorkFolder is not specified", true);
                scaffoldSeedersWorkFolderSpecified = false;
            }

            //პროექტების ჩამონათვალი
            foreach (var kvp in parameters.Projects.OrderBy(o => o.Key))
            {
                SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.Main);
                if (scaffoldSeedersWorkFolderSpecified)
                    SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.ScaffoldSeed);
            }


        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
    }

    private void SyncAllGitsForOneProject(string projectName, ProjectModel project, EGitCol gitCol)
    {
        switch (gitCol)
        {
            case EGitCol.Main:
                if (!string.IsNullOrWhiteSpace(project.ProjectFolderName))
                    break;
                StShared.WriteErrorLine($"ProjectFolderName is not specified for project {projectName}", true);
                return;
            case EGitCol.ScaffoldSeed:
                if (!string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
                    break;
                StShared.WriteWarningLine($"ScaffoldSeederProjectName is not specified for project {projectName}",
                    true);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(gitCol), gitCol, null);
        }

        var syncAllGitsCliMenuCommandMain =
            new SyncAllGitsCliMenuCommand(_logger, _parametersManager, projectName, gitCol, false, false);
        syncAllGitsCliMenuCommandMain.Run();
    }

}