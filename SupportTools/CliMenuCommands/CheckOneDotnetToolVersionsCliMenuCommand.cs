using CliMenu;
using ParametersManagement.LibParameters;
using SupportTools.Tools;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class CheckOneDotnetToolVersionsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _toolKey;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckOneDotnetToolVersionsCliMenuCommand(IParametersManager parametersManager, string toolKey) : base(
        "Check One Dotnet Tools Versions...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _toolKey = toolKey;
    }

    protected override bool RunBody()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        if (DotnetToolsVersionsCheckerUpdater.CheckOne(_parametersManager, _toolKey))
            //შენახვა
            _parametersManager.Save(parameters, "Dotnet Tools versions checked success");
        return true;
    }
}