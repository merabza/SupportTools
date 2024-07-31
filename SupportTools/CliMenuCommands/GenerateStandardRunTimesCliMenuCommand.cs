using CliMenu;
using LibDataInput;
using LibFileParameters.Interfaces;
using LibParameters;
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

    protected override bool RunBody()
    {
        var parameters = (IParametersWithSmartSchemas)_parametersManager.Parameters;

        if (!Inputer.InputBool("This process will change RunTimes, are you sure?", false, false))
            return false;

        StandardRunTimesGenerator.Generate(_parametersManager);

        //შენახვა
        _parametersManager.Save(parameters, "RunTimes generated success");
        return true;
    }
}