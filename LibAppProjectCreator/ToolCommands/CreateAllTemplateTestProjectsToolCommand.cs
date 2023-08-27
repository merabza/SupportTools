using System;
using CliParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;

namespace LibAppProjectCreator.ToolCommands;

public sealed class CreateAllTemplateTestProjectsToolCommand : ToolCommand
{
    public CreateAllTemplateTestProjectsToolCommand(ILogger logger, string actionName,
        ParametersManager parametersManager) : base(logger, actionName, parametersManager)
    {
    }

    protected override bool RunAction()
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
                new(Logger, ParametersManager, kvp.Key, ETestOrReal.Test);

            appProjectCreatorByTemplate.Run();

            Console.WriteLine("Finished create test Project: {0}", kvp.Key);
        }

        return true;
    }
}