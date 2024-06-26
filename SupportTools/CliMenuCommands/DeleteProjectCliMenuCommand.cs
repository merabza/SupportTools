﻿using CliMenu;
using LibDataInput;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class DeleteProjectCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteProjectCliMenuCommand(ParametersManager parametersManager, string projectName) : base("Delete Project",
        EMenuAction.LevelUp, EMenuAction.Reload, projectName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override bool RunBody()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var projects = parameters.Projects;
        if (!projects.ContainsKey(_projectName))
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return false;
        }

        if (!Inputer.InputBool($"This will Delete Project {_projectName}. are you sure?", false, false))
            return false;

        projects.Remove(_projectName);
        _parametersManager.Save(parameters, $"Project {_projectName} Deleted");
        MenuAction = EMenuAction.LevelUp;
        return true;
    }
}