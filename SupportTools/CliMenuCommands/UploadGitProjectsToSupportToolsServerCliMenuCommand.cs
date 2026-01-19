using System.Net.Http;
using System.Threading;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class UploadGitProjectsToSupportToolsServerCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UploadGitProjectsToSupportToolsServerCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base(UploadGitProjectsToSupportToolsServerToolAction.ActionName,
        EMenuAction.Reload)
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