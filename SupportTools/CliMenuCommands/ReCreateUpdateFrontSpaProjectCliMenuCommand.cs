using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibAppProjectCreator.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateUpdateFrontSpaProjectCliMenuCommand : CliMenuCommand
{
    private readonly ReCreateUpdateFrontSpaProjectToolAction _reCreateUpdateFrontSpaProjectToolAction;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReCreateUpdateFrontSpaProjectCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base("ReCreate Update Front Spa Project", EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _reCreateUpdateFrontSpaProjectToolAction =
            new ReCreateUpdateFrontSpaProjectToolAction(logger, httpClientFactory, parametersManager, true);
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(AppProjectCreatorByTemplateToolAction.ActionDescription);
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        string? projectName = MenuSet?.ParentMenu?.Caption;
        if (string.IsNullOrEmpty(projectName))
        {
            Console.WriteLine("Project name is not set in menu caption.");
            return false;
        }

        _reCreateUpdateFrontSpaProjectToolAction.SetProjectName(projectName);
        return await _reCreateUpdateFrontSpaProjectToolAction.Run(cancellationToken);
    }
}
