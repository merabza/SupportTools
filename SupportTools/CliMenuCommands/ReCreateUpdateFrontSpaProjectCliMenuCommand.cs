using System.Net.Http;
using System.Threading;
using CliMenu;
using LibAppProjectCreator.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateUpdateFrontSpaProjectCliMenuCommand : CliMenuCommand
{
    private readonly ReCreateUpdateFrontSpaProjectToolAction _reCreateUpdateFrontSpaProjectToolAction;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReCreateUpdateFrontSpaProjectCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string projectName) : base("ReCreate Update Front Spa Project",
        EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _reCreateUpdateFrontSpaProjectToolAction =
            new ReCreateUpdateFrontSpaProjectToolAction(logger, httpClientFactory, parametersManager, projectName,
                true);
    }

    protected override string GetActionDescription()
    {
        return AppProjectCreatorByTemplateToolAction.ActionDescription;
    }

    protected override bool RunBody()
    {
        return _reCreateUpdateFrontSpaProjectToolAction.Run(CancellationToken.None).Result;
    }
}