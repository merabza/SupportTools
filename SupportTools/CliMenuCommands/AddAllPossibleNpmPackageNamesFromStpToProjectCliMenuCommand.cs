using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class AddAllPossibleNpmPackageNamesFromStpToProjectCliMenuCommand : CliMenuCommand
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

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        List<string> npmPackageNamesFromProject = parameters.GetNpmPackageNames(_projectName);

        foreach (KeyValuePair<string, string> kvp in parameters.NpmPackages.Where(kvp =>
                     !npmPackageNamesFromProject.Contains(kvp.Key)))
        {
            npmPackageNamesFromProject.Add(kvp.Key);
        }

        //ცვლილებების შენახვა
        await _parametersManager.Save(parameters, "Add Npm Packages Finished", null, cancellationToken);

        return true;
    }
}
