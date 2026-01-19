using System.Net.Http;
using System.Threading;
using AppCliTools.CliMenu;
using LibAppProjectCreator.ToolCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class CreateAllTemplateTestProjectsCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreateAllTemplateTestProjectsCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager) : base("Create All Template Test Projects", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _httpClientFactory = httpClientFactory;
    }

    protected override bool RunBody()
    {
        var createAllTemplateTestProjectsToolCommand =
            new CreateAllTemplateTestProjectsToolCommand(_logger, _httpClientFactory, Name, _parametersManager, true);
        return createAllTemplateTestProjectsToolCommand.Run(CancellationToken.None).Result;
    }
}