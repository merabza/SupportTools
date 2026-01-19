using CliMenu;
using LibDataInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class DeleteTemplateCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _templateName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteTemplateCliMenuCommand(ParametersManager parametersManager, string templateName) : base(
        "Delete Template", EMenuAction.LevelUp, EMenuAction.Reload, templateName)
    {
        _parametersManager = parametersManager;
        _templateName = templateName;
    }

    protected override bool RunBody()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        var parameters = supportToolsParameters.AppProjectCreatorAllParameters;
        if (parameters == null)
        {
            StShared.WriteErrorLine("Support Tools Parameters not found", true);
            return false;
        }

        var templates = parameters.Templates;
        if (!templates.ContainsKey(_templateName))
        {
            StShared.WriteErrorLine($"Template {_templateName} not found", true);
            return false;
        }

        if (!Inputer.InputBool($"This will Delete Template {_templateName}. are you sure?", false, false))
            return false;

        templates.Remove(_templateName);
        _parametersManager.Save(parameters, $"Template {_templateName} Deleted");
        MenuAction = EMenuAction.LevelUp;
        return true;
    }
}