using System;
using System.Threading;
using CliMenu;
using LibAppProjectCreator;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class TemplateRunCliMenuCommand : CliMenuCommand
{
    private readonly AppProjectCreatorByTemplateToolAction _appProjectCreatorByTemplate;

    public TemplateRunCliMenuCommand(ILogger logger, ParametersManager parametersManager, string templateName,
        ETestOrReal testOrReal)
    {
        _appProjectCreatorByTemplate =
            new AppProjectCreatorByTemplateToolAction(logger, parametersManager, templateName, testOrReal);
    }

    protected override string GetActionDescription()
    {
        return AppProjectCreatorByTemplateToolAction.ActionDescription;
    }

    protected override void RunAction()
    {
        try
        {
            MenuAction = EMenuAction.Reload;

            //დავინიშნოთ დრო
            var startDateTime = DateTime.Now;
            Console.WriteLine("Task is running...");
            Console.WriteLine("---");

            MenuAction = EMenuAction.Reload;

            _appProjectCreatorByTemplate.Run(CancellationToken.None).Wait();

            Console.WriteLine("---");

            Console.WriteLine($"Task Finished. {StShared.TimeTakenMessage(startDateTime)}");
            StShared.Pause();
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
            StShared.Pause();
        }
    }
}