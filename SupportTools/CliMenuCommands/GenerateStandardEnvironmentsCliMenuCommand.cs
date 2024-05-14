﻿using CliMenu;
using LibDataInput;
using LibFileParameters.Interfaces;
using LibParameters;
using SupportTools.Generators;

namespace SupportTools.CliMenuCommands;

public sealed class GenerateStandardEnvironmentsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GenerateStandardEnvironmentsCliMenuCommand(IParametersManager parametersManager)
    {
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;
        var parameters = (IParametersWithSmartSchemas)_parametersManager.Parameters;

        if (!Inputer.InputBool("This process will change Environments, are you sure?", false, false))
            return;

        StandardEnvironmentsGenerator.Generate(_parametersManager);

        //შენახვა
        _parametersManager.Save(parameters, "Environments generated success");
    }
}