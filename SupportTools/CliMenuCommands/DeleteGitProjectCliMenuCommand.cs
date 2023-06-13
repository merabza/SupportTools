using System;
using CliMenu;
using LibDataInput;
using LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class DeleteGitProjectCliMenuCommand : CliMenuCommand
{
    private readonly string _gitProjectName;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    public DeleteGitProjectCliMenuCommand(ParametersManager parametersManager, string projectName,
        string gitProjectName) :
        base(
            "Delete Git Project", gitProjectName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
    }

    protected override void RunAction()
    {
        try
        {
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var gitProjectNames = parameters.GetGitProjectNames(_projectName, EGitCol.Main);

            if (!gitProjectNames.Contains(_gitProjectName))
            {
                StShared.WriteErrorLine(
                    $"Git Project with name {_gitProjectName} does not exists in project {_projectName}", true);
                return;
            }

            if (!Inputer.InputBool(
                    $"This will Delete Git Project with Name {_gitProjectName}, from this project. are you sure?",
                    false, false))
                return;

            gitProjectNames.Remove(_gitProjectName);
            _parametersManager.Save(parameters,
                $"Git Project with Name {_gitProjectName} in project {_projectName} deleted.");

            MenuAction = EMenuAction.LevelUp;
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

        MenuAction = EMenuAction.Reload;
    }
}