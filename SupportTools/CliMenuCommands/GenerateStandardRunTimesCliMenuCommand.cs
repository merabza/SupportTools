using System;
using CliMenu;
using LibDataInput;
using LibFileParameters.Interfaces;
using LibParameters;
using SupportTools.Generators;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class GenerateStandardRunTimesCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateStandardRunTimesCliMenuCommand(IParametersManager parametersManager)
    {
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;
        var parameters = (IParametersWithSmartSchemas)_parametersManager.Parameters;
        try
        {
            if (!Inputer.InputBool("This process will change RunTimes, are you sure?", false, false))
                return;

            StandardRunTimesGenerator.Generate(_parametersManager);

            //შენახვა
            _parametersManager.Save(parameters, "RunTimes generated success");
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
    }
}