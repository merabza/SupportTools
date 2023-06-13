using System;
using CliMenu;
using LibDataInput;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class DeleteTemplateCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _templateName;

    public DeleteTemplateCliMenuCommand(ParametersManager parametersManager, string templateName) : base(
        "Delete Template", templateName)
    {
        _parametersManager = parametersManager;
        _templateName = templateName;
    }

    protected override void RunAction()
    {
        try
        {
            var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
            var parameters = supportToolsParameters.AppProjectCreatorAllParameters;
            if (parameters == null)
            {
                StShared.WriteErrorLine("Support Tools Parameters not found", true);
                return;
            }

            var templates = parameters.Templates;
            if (!templates.ContainsKey(_templateName))
            {
                StShared.WriteErrorLine($"Template {_templateName} not found", true);
                return;
            }

            if (!Inputer.InputBool($"This will Delete Template {_templateName}. are you sure?", false, false))
                return;

            templates.Remove(_templateName);
            _parametersManager.Save(parameters, $"Template {_templateName} Deleted");
            MenuAction = EMenuAction.LevelUp;
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