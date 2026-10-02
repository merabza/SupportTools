using System;
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
    private readonly Func<string, bool> _confirm;
    private readonly IParametersManager _parametersManager;
    private readonly string _templateName;

    public DeleteTemplateCliMenuCommand(IParametersManager parametersManager, string templateName) : this(
        parametersManager, templateName, question => Inputer.InputBool(question, false, false))
    {
    }

    //კონსოლიდან დასტური პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal DeleteTemplateCliMenuCommand(IParametersManager parametersManager, string templateName,
        Func<string, bool> confirm) : base("Delete Template", EMenuAction.LevelUp, EMenuAction.Reload, templateName)
    {
        _parametersManager = parametersManager;
        _templateName = templateName;
        _confirm = confirm;
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

        if (!_confirm($"This will Delete Template {_templateName}. are you sure?"))
        {
            return false;
        }

        templates.Remove(_templateName);
        //ინახება ძირეული ობიექტი: შაბლონების ობიექტის შენახვა მთელ ფაილს მხოლოდ მისით გადაწერდა
        await _parametersManager.Save(supportToolsParameters, $"Template {_templateName} Deleted", null,
            cancellationToken);
        MenuAction = EMenuAction.LevelUp;
        return true;
    }
}
