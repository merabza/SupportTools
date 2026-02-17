using System.Threading;
using System.Threading.Tasks;
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

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(_command.Description);
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        return await _command.Run(cancellationToken);
    }
}
