using System;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace LibAppProjectCreator.ToolCommands;

public sealed class ReCreateAllReactAppsByTemplatesToolCommand : ToolCommand
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReCreateAllReactAppsByTemplatesToolCommand(ILogger logger, string actionName, IParameters par,
        IParametersManager? parametersManager) : base(logger, actionName, par, parametersManager)
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

        foreach (var kvp in parameters.ReactAppTemplates)
        {
            Console.WriteLine("Start create React App: {0}", kvp.Key);

            var command =
                new ReCreateReactAppFilesByTemplateNameToolCommand(_logger, kvp.Key, kvp.Value, parameters,
                    ParametersManager);

            await command.Run(cancellationToken);

            Console.WriteLine("Finished create React App: {0}", kvp.Key);
        }

        return true;
    }
}