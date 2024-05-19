﻿using CliMenu;
using LibAppProjectCreator.ToolCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Threading;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class CreateAllTemplateTestProjectsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    public CreateAllTemplateTestProjectsCliMenuCommand(ILogger logger, ParametersManager parametersManager) : base(
        "Create All Template Test Projects", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        CreateAllTemplateTestProjectsToolCommand createAllTemplateTestProjectsToolCommand =
            new(_logger, Name!, _parametersManager);
        return createAllTemplateTestProjectsToolCommand.Run(CancellationToken.None).Result;
    }
}