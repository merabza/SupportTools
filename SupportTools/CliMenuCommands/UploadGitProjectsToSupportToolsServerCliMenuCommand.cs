using System.Net.Http;
using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace SupportTools.CliMenuCommands;

public sealed class UploadGitProjectsToSupportToolsServerCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadGitProjectsToSupportToolsServerCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory, IParametersManager parametersManager) :
        base(UploadGitProjectsToSupportToolsServerToolAction.ActionName, EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var uploadGitProjectsToSupportToolsServerToolAction =
            new UploadGitProjectsToSupportToolsServerToolAction(_logger, _httpClientFactory, _parametersManager, true);
        return uploadGitProjectsToSupportToolsServerToolAction.Run(CancellationToken.None).Result;
    }
}