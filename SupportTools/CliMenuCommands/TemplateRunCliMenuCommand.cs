using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibAppProjectCreator.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;

namespace SupportTools.CliMenuCommands;

public sealed class TemplateRunCliMenuCommand : CliMenuCommand
{
    private readonly AppProjectCreatorByTemplateToolAction _appProjectCreatorByTemplate;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TemplateRunCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string templateName, ETestOrReal testOrReal) : base(
        $"Create {testOrReal} Project by {templateName} Template", EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _appProjectCreatorByTemplate = new AppProjectCreatorByTemplateToolAction(logger, httpClientFactory,
            parametersManager, templateName, testOrReal, true);
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(AppProjectCreatorByTemplateToolAction.ActionDescription);
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        return await _appProjectCreatorByTemplate.Run(cancellationToken);
    }
}
