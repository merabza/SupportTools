using CliMenu;
using LibDataInput;
using LibParameters;
using SupportTools.Tools;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateAllToolsToLatestVersionCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateAllToolsToLatestVersionCliMenuCommand(IParametersManager parametersManager) : base(
        "Update All Tools To Latest Version", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        return Inputer.InputBool("Are you sure, you want to Update All Tools To Latest Version?", true, false) &&
               DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager);
    }
}