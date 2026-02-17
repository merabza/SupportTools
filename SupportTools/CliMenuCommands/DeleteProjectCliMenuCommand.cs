using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

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

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        Dictionary<string, ProjectModel> projects = parameters.Projects;
        if (!projects.ContainsKey(_projectName))
        {
            StShared.WriteErrorLine($"Project {_projectName} does not found", true);
            return ValueTask.FromResult(false);
        }

        if (!Inputer.InputBool($"This will Delete Project {_projectName}. are you sure?", false, false))
        {
            return ValueTask.FromResult(false);
        }

        projects.Remove(_projectName);
        _parametersManager.Save(parameters, $"Project {_projectName} Deleted");
        MenuAction = EMenuAction.LevelUp;
        return ValueTask.FromResult(true);
    }
}
