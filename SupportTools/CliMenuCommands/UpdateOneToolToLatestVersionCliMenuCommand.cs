﻿using CliMenu;
using LibDataInput;
using LibParameters;
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

    protected override bool RunBody()
    {
        return Inputer.InputBool("Are you sure, you want to Update All Tools To Latest Version?", true, false) &&
               DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager, _toolKey);
    }
}