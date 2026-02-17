using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class DeleteNpmPackageFromProjectCliMenuCommand : CliMenuCommand
{
    private readonly string _npmPackageName;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteNpmPackageFromProjectCliMenuCommand(ParametersManager parametersManager, string projectName,
        string npmPackageName) : base("Delete Npm Package from project", EMenuAction.LevelUp, EMenuAction.Reload,
        npmPackageName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _npmPackageName = npmPackageName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        List<string> npmPackageNames = parameters.GetNpmPackageNames(_projectName);

        if (!npmPackageNames.Contains(_npmPackageName))
        {
            StShared.WriteErrorLine(
                $"Npm Package with name {_npmPackageName} does not exists in project {_projectName}", true);
            return ValueTask.FromResult(false);
        }

        if (!Inputer.InputBool(
                $"This will Delete Npm Package with Name {_npmPackageName}, from this project. are you sure?", false,
                false))
        {
            return ValueTask.FromResult(false);
        }

        npmPackageNames.Remove(_npmPackageName);
        //if (!parameters.DeleteNpmPackageFromProjectByNames(_projectName, _npmPackageName))
        //{
        //    StShared.WriteErrorLine($"git project {_npmPackageName} is not removed from project {_projectName}", true);
        //    return false;
        //}

        _parametersManager.Save(parameters,
            $"Npm Package with Name {_npmPackageName} in project {_projectName} is deleted.");

        MenuAction = EMenuAction.LevelUp;
        return ValueTask.FromResult(true);
    }
}
