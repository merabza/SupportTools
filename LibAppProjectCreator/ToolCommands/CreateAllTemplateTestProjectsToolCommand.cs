﻿using System;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.ToolCommands;

public sealed class CreateAllTemplateTestProjectsToolCommand : ToolCommand
{
    private readonly ILogger _logger;

    public CreateAllTemplateTestProjectsToolCommand(ILogger logger, string actionName,
        ParametersManager parametersManager, bool useConsole) : base(logger, actionName, parametersManager, null,
        useConsole)
    {
        _logger = logger;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var parameters = (SupportToolsParameters?)ParametersManager?.Parameters;

        if (ParametersManager is null)
            return false;

        if (parameters is null)
            return false;

        if (parameters.AppProjectCreatorAllParameters is null)
            return true;

        foreach (var kvp in parameters.AppProjectCreatorAllParameters.Templates)
        {
            Console.WriteLine("Start create test Project: {0}", kvp.Key);

            AppProjectCreatorByTemplateToolAction appProjectCreatorByTemplate =
                new(_logger, ParametersManager, kvp.Key, ETestOrReal.Test, UseConsole);

            await appProjectCreatorByTemplate.Run(cancellationToken);

            Console.WriteLine("Finished create test Project: {0}", kvp.Key);
        }

        return true;
    }
}