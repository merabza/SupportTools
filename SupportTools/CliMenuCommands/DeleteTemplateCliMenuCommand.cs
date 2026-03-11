using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
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

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        AppProjectCreatorAllParameters? parameters = supportToolsParameters.AppProjectCreatorAllParameters;
        if (parameters == null)
        {
            StShared.WriteErrorLine("Support Tools Parameters not found", true);
            return false;
        }

        Dictionary<string, TemplateModel> templates = parameters.Templates;
        if (!templates.ContainsKey(_templateName))
        {
            StShared.WriteErrorLine($"Template {_templateName} not found", true);
            return false;
        }

        if (!Inputer.InputBool($"This will Delete Template {_templateName}. are you sure?", false, false))
        {
            return false;
        }

        templates.Remove(_templateName);
        await _parametersManager.Save(parameters, $"Template {_templateName} Deleted", null, cancellationToken);
        MenuAction = EMenuAction.LevelUp;
        return true;
    }
}
