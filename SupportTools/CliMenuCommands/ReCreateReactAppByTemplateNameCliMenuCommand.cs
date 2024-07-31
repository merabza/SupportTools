using System.Threading;
using CliMenu;
using LibAppProjectCreator.ToolCommands;
using LibFileParameters.Interfaces;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateReactAppByTemplateNameCliMenuCommand : CliMenuCommand
{
    private readonly ReCreateReactAppFilesByTemplateNameToolCommand _command;

    public ReCreateReactAppByTemplateNameCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string reactAppName, string? reactTemplateName) : base("Recreate React App files By Template...",
        EMenuAction.Reload)
    {
        var parameters = (IParametersWithSmartSchemas)parametersManager.Parameters;

        _command = new ReCreateReactAppFilesByTemplateNameToolCommand(logger, reactAppName, reactTemplateName,
            parameters, parametersManager);
    }

    protected override string? GetActionDescription()
    {
        return _command.Description;
    }

    protected override bool RunBody()
    {
        return _command.Run(CancellationToken.None).Result;
    }
}