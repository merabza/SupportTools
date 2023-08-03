using System;
using CliMenu;
using LibDataInput;
using LibFileParameters.Interfaces;
using LibParameters;
using SupportTools.Generators;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class GenerateStandardEnvironmentsCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    public GenerateStandardEnvironmentsCommand(IParametersManager parametersManager)
    {
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;
        var parameters = (IParametersWithSmartSchemas)_parametersManager.Parameters;
        try
        {
            if (!Inputer.InputBool("This process will change Environments, are you sure?", false, false))
                return;

            StandardEnvironmentsGenerator.Generate(_parametersManager);

            //შენახვა
            _parametersManager.Save(parameters, "Environments generated success");
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