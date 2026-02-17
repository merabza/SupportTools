using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using ParametersManagement.LibFileParameters.Interfaces;
using ParametersManagement.LibParameters;
using SupportTools.Generators;

namespace SupportTools.CliMenuCommands;

public sealed class GenerateStandardRunTimesCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateStandardRunTimesCliMenuCommand(IParametersManager parametersManager) : base(
        "Generate standard RunTimes...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (IParametersWithSmartSchemas)_parametersManager.Parameters;

        if (!Inputer.InputBool("This process will change RunTimes, are you sure?", false, false))
        {
            return ValueTask.FromResult(false);
        }

        StandardRunTimesGenerator.Generate(_parametersManager);

        //შენახვა
        _parametersManager.Save(parameters, "RunTimes generated success");
        return ValueTask.FromResult(true);
    }
}
