using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;

public sealed class SelectProjectAllowToolsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly Action<string, Dictionary<string, bool>> _selectTools;

    public SelectProjectAllowToolsCliMenuCommand(IParametersManager parametersManager, string projectName) : this(
        parametersManager, projectName, (caption, checks) => MenuInputer.MultipleInputFromList(caption, checks))
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა არჩევანი თვითონ მიაწოდონ (checks-ს ადგილზე ცვლის)
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SelectProjectAllowToolsCliMenuCommand(IParametersManager parametersManager, string projectName,
        Action<string, Dictionary<string, bool>> selectTools) : base("Select Allow tools...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _selectTools = selectTools;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"Project with name {_projectName} does not exists", true);
            return false;
        }

        //ჩანს მხოლოდ პროექტის ტიპისა და UsesDatabase-ის მიხედვით ხილული ინსტრუმენტები. დამალულის არჩევა არ იცვლება
        List<EProjectTools> visibleTools = [.. Enum.GetValues<EProjectTools>().Where(project.IsToolVisible)];

        //დადგინდეს ამ ფოლდერებიდან რომელიმე არის თუ არა დასაბექაპებელ სიაში. და თუ არის მისთვის ჩაირთოს ჭეშმარიტი
        Dictionary<string, bool> checks =
            visibleTools.ToDictionary(tool => tool.GetProjectToolName(), tool => project.AllowToolsList.Contains(tool));

        Dictionary<string, EProjectTools> visibleNames = visibleTools
            .ToDictionary(tool => tool.GetProjectToolName(), tool => tool);

        //გამოვიდეს სიიდან ამრჩევი
        _selectTools("Select allow tools", checks);

        foreach (KeyValuePair<string, bool> kvp in checks)
        {
            if (!visibleNames.TryGetValue(kvp.Key, out EProjectTools tool))
            {
                return false;
            }

            if (kvp.Value)
            {
                //ჩართული ჩავამატოთ თუ არ არსებობს
                if (!project.AllowToolsList.Contains(tool))
                {
                    project.AllowToolsList.Add(tool);
                }
            }
            else
            {
                //გამორთული ამოვაკლოთ თუ არსებობს
                project.AllowToolsList.Remove(tool);
            }
        }

        await _parametersManager.Save(parameters, "Changes saved", null, cancellationToken);
        return true;
    }
}
