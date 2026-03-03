using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class NpmPackageInProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly string _npmPackageName;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NpmPackageInProjectSubMenuCliMenuCommand(ParametersManager parametersManager, string projectName,
        string npmPackageName) : base(npmPackageName, EMenuAction.LoadSubMenu)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _npmPackageName = npmPackageName;
    }

    public override CliMenuSet GetSubMenu()
    {
        var gitProjectSubMenuSet = new CliMenuSet(_npmPackageName);

        //Npm პროექტის წაშლა ამ პროექტიდან
        var deleteGitProjectCommand =
            new DeleteNpmPackageFromProjectCliMenuCommand(_parametersManager, _projectName, _npmPackageName);
        gitProjectSubMenuSet.AddMenuItem(deleteGitProjectCommand);

        //პროექტის მენიუში დაბრუნება
        gitProjectSubMenuSet.AddEscapeCommand("Exit to Git menu");

        return gitProjectSubMenuSet;
    }
}
