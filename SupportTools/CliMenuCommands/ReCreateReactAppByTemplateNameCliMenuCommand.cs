using CliMenu;
using LibAppProjectCreator.ToolCommands;
using LibFileParameters.Interfaces;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateReactAppByTemplateNameCliMenuCommand : CliMenuCommand
{
    private readonly ReCreateReactAppFilesByTemplateNameToolCommand _command;

    public ReCreateReactAppByTemplateNameCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string reactAppName, string? reactTemplateName)
    {
        var parameters = (IParametersWithSmartSchemas)parametersManager.Parameters;

        _command = new ReCreateReactAppFilesByTemplateNameToolCommand(logger, reactAppName, reactTemplateName,
            parameters, parametersManager);
    }

    protected override string? GetActionDescription()
    {
        return _command.Description;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        _command.Run(CancellationToken.None).Wait();
    }
}