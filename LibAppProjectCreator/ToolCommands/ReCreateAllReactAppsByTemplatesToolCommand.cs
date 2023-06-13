using System;
using CliParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace LibAppProjectCreator.ToolCommands;

public sealed class ReCreateAllReactAppsByTemplatesToolCommand : ToolCommand
{
    public ReCreateAllReactAppsByTemplatesToolCommand(ILogger logger, bool useConsole, string actionName,
        IParameters par, IParametersManager? parametersManager) : base(logger, useConsole, actionName, par,
        parametersManager)
    {
    }


    protected override bool RunAction()
    {
        var parameters = (SupportToolsParameters?)ParametersManager?.Parameters;

        if (ParametersManager is null)
            return false;

        if (parameters is null)
            return false;

        foreach (var kvp in parameters.ReactAppTemplates)
        {
            Console.WriteLine("Start create React App: {0}", kvp.Key);

            var command = new ReCreateReactAppFilesByTemplateNameToolCommand(Logger, true, kvp.Key, kvp.Value,
                parameters, ParametersManager);

            command.Run();

            Console.WriteLine("Finished create React App: {0}", kvp.Key);
        }

        return true;
    }
}