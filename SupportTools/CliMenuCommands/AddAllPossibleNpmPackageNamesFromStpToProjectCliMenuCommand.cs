using System.Linq;
using CliMenu;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public class AddAllPossibleNpmPackageNamesFromStpToProjectCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AddAllPossibleNpmPackageNamesFromStpToProjectCliMenuCommand(ParametersManager parametersManager,
        string projectName) : base("Add All Possible Npm Package Names From STP To Project", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override bool RunBody()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var npmPackageNamesFromProject = parameters.GetNpmPackageNames(_projectName);

        foreach (var kvp in parameters.NpmPackages.Where(kvp => !npmPackageNamesFromProject.Contains(kvp.Key)))
            npmPackageNamesFromProject.Add(kvp.Key);

        //ცვლილებების შენახვა
        _parametersManager.Save(parameters, "Add Npm Packages Finished");

        return true;
    }
}