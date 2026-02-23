using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibFileParameters.Interfaces;
using ParametersManagement.LibParameters;
using SupportTools.Generators;

namespace SupportTools.CliMenuCommands;

public sealed class GenerateStandardEnvironmentsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateStandardEnvironmentsCliMenuCommand(IParametersManager parametersManager) : base(
        "Generate standard Environments...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (IParametersWithSmartSchemas)_parametersManager.Parameters;

        if (!Inputer.InputBool("This process will change Environments, are you sure?", false, false))
        {
            return false;
        }

        StandardEnvironmentsGenerator.Generate(_parametersManager);

        //შენახვა
        await _parametersManager.Save(parameters, "Environments generated success", null, cancellationToken);
        return true;
    }
}
