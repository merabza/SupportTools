using System;
using CliMenu;
using LibAppProjectCreator.ToolCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

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

    protected override string GetActionDescription()
    {
        return Name!;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        try
        {
            CreateAllTemplateTestProjectsToolCommand createAllTemplateTestProjectsToolCommand =
                new(_logger, true, Name!, _parametersManager);
            createAllTemplateTestProjectsToolCommand.Run();

            StShared.Pause();

            return;
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }

        MenuAction = EMenuAction.Reload;
    }
}