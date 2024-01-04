using System;
using System.Linq;
using System.Threading;
using CliMenu;
using LibAppProjectCreator.Git;
using LibAppProjectCreator.Models;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class SyncAllGitsCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncAllGitsCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        EGitCol gitCol) : base("Sync All", null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitCol = gitCol;
    }

    protected override void RunAction()
    {
        try
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;
            var project = parameters.GetProject(_projectName);

            if (project == null)
            {
                StShared.WriteErrorLine("project does not found", true);
                return;
            }

            //if (string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
            //{
            //    StShared.WriteErrorLine($"ScaffoldSeederProjectName does not specified for Project {_projectName}", true);
            //    return;
            //}

            var gitsFolder = parameters.GetGitsFolder(_projectName, _gitCol);

            if (gitsFolder == null)
            {
                StShared.WriteErrorLine("Gits folder does not found", true);
                return;
            }

            var gitProjectNames = _gitCol switch
            {
                EGitCol.Main => project.GitProjectNames,
                EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
                _ => null
            } ?? [];

            var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);

            var gitRepos = GitRepos.Create(_logger, parameters.Gits, project.MainProjectFolderRelativePath(gitProjects),
                project.SpaProjectFolderRelativePath(gitProjects));

            //var absentGitRepoNames = gitRepos.Gits.Where(x => !gitProjectNames.Contains(x.Key)).Select(x => x.Key).ToList();

            var absentGitRepoNames = gitProjectNames.Except(gitRepos.Gits.Keys).ToList();


            if (absentGitRepoNames.Count != 0)
            {
                foreach (var absentGitRepoName in absentGitRepoNames)
                    StShared.WriteErrorLine(absentGitRepoName, true, null, false);
                StShared.WriteErrorLine("Gits with this names are absent", true);
            }


            GitSyncAll gitSyncAll = new(_logger, gitsFolder,
                gitRepos.Gits.Where(x => gitProjectNames.Contains(x.Key)).Select(x => x.Value));

            gitSyncAll.Run(CancellationToken.None).Wait();

            MenuAction = EMenuAction.LevelUp;
            Console.WriteLine("Success");
            return;
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
        finally
        {
            StShared.Pause();
        }

        MenuAction = EMenuAction.Reload;
    }
}