using CliMenu;
using LibAppProjectCreator.ToolCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Threading;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class CreateAllTemplateTestProjectsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    public CreateAllTemplateTestProjectsCliMenuCommand(ILogger logger, ParametersManager parametersManager) : base(
        "Create All Template Test Projects")
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        CreateAllTemplateTestProjectsToolCommand createAllTemplateTestProjectsToolCommand =
            new(_logger, Name!, _parametersManager);
        createAllTemplateTestProjectsToolCommand.Run(CancellationToken.None).Wait();
    }
}