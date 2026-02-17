using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibParameters;
using SupportTools.Tools;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateOneToolToLatestVersionCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _toolKey;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOneToolToLatestVersionCliMenuCommand(IParametersManager parametersManager, string toolKey) : base(
        "Update All Tools To Latest Version", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _toolKey = toolKey;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            Inputer.InputBool("Are you sure, you want to Update All Tools To Latest Version?", true, false) &&
            DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager, _toolKey));
    }
}
