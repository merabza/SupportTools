using CliMenu;
using LibDataInput;
using LibGitData;
using LibParameters;
using SupportToolsData.Models;
using System.Collections.Generic;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class DeleteGitProjectCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteGitProjectCliMenuCommand(ParametersManager parametersManager, string projectName,
        string gitProjectName, EGitCol gitCol) :
        base(
            "Delete Git Project", gitProjectName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
    }

    protected override void RunAction()
    {

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

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
            StShared.WriteErrorLine(
                $"Git Project with name {_gitProjectName} does not exists in project {_projectName}", true);
            return;
        }

        if (!Inputer.InputBool(
                $"This will Delete Git Project with Name {_gitProjectName}, from this project. are you sure?",
                false, false))
            return;

        gitProjectNames.Remove(_gitProjectName);
        if (!parameters.DeleteGitFromProjectByNames(_projectName, _gitProjectName, _gitCol))
        {
            StShared.WriteErrorLine($"git project {_gitProjectName} is not removed from project {_projectName}",
                true);
            return;
        }

        _parametersManager.Save(parameters,
            $"Git Project with Name {_gitProjectName} in project {_projectName} deleted.");

        MenuAction = EMenuAction.LevelUp;
    }
}