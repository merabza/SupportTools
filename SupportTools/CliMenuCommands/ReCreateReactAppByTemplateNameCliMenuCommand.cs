using System.Threading;
using AppCliTools.CliMenu;
using LibAppProjectCreator.ToolCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Interfaces;
using ParametersManagement.LibParameters;

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

    protected override string GetActionDescription()
    {
        return _command.Description;
    }

    protected override bool RunBody()
    {
        return _command.Run(CancellationToken.None).Result;
    }
}