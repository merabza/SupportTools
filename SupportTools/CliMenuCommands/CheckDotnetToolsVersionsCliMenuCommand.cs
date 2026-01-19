using CliMenu;
using ParametersManagement.LibParameters;
using SupportTools.Tools;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class CheckDotnetToolsVersionsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckDotnetToolsVersionsCliMenuCommand(IParametersManager parametersManager) : base(
        "Check Dotnet Tools Versions...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        if (DotnetToolsVersionsCheckerUpdater.Check(_parametersManager))
            //შენახვა
            _parametersManager.Save(parameters, "Dotnet Tools versions checked success");
        return true;
    }
}