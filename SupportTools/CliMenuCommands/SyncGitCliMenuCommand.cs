using System;
using System.Collections.Generic;
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

public sealed class SyncGitCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncGitCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        string gitProjectName, EGitCol gitCol) : base("Sync")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
    }

    protected override void RunAction()
    {
        try
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var project = parameters.GetProject(_projectName);
            if (project is null)
            {
                StShared.WriteErrorLine(
                    $"Git Project with name {_projectName} does not exists", true);
                return;
            }

            var result = parameters.GetGitProjectNames(_projectName, _gitCol);
            if (result.IsNone)
            {
                StShared.WriteErrorLine(
                    $"Git Project with name {_projectName} does not exists", true);
                return;
            }

            var gitProjectNames = (List<string>)result;

            if (!gitProjectNames.Contains(_gitProjectName))
            {
                StShared.WriteErrorLine($"Git Project with name {_gitProjectName} does not exists", true, _logger);
                return;
            }

            if (!parameters.Gits.ContainsKey(_gitProjectName))
            {
                StShared.WriteErrorLine(
                    $"Git Project with name {_gitProjectName} does not exists in project {_projectName}", true,
                    _logger);
                return;
            }

            var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);

            var gitRepos = GitRepos.Create(_logger, parameters.Gits, project.MainProjectFolderRelativePath(gitProjects),
                project.SpaProjectFolderRelativePath(gitProjects));

            if (!gitRepos.Gits.ContainsKey(_gitProjectName))
            {
                StShared.WriteErrorLine($"Git Project with name {_gitProjectName} does not exists in gits list", true,
                    _logger);
                return;
            }

            var gitsFolder = parameters.GetGitsFolder(_projectName, _gitCol);

            if (gitsFolder == null)
            {
                StShared.WriteErrorLine("Gits folder not found", true);
                return;
            }


            if (!Inputer.InputBool($"This process will Sync git {_gitProjectName}, are you sure?", true, false))
            {
                MenuAction = EMenuAction.Reload;
                return;
            }

            GitSync gitSync = new(_logger, gitsFolder, gitRepos.Gits[_gitProjectName]);
            gitSync.Run(CancellationToken.None).Wait();

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