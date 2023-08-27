using System;
using CliMenu;
using LibAppProjectCreator.ToolCommands;
using LibDataInput;
using LibFileParameters.Interfaces;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateReactAppByTemplateNameCommand : CliMenuCommand
{
    private readonly ReCreateReactAppFilesByTemplateNameToolCommand _command;

    public ReCreateReactAppByTemplateNameCommand(ILogger logger, IParametersManager parametersManager,
        string reactAppName, string? reactTemplateName)
    {
        var parameters = (IParametersWithSmartSchemas)parametersManager.Parameters;

        _command = new ReCreateReactAppFilesByTemplateNameToolCommand(logger, reactAppName, reactTemplateName,
            parameters, parametersManager);
    }

    protected override string? GetActionDescription()
    {
        return _command.Description;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;
        try
        {
            _command.Run();
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
        }
    }
}