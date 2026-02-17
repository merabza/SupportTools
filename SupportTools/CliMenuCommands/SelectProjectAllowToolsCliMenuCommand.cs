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

namespace SupportTools.CliMenuCommands;

public sealed class SelectProjectAllowToolsCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SelectProjectAllowToolsCliMenuCommand(ParametersManager parametersManager, string projectName) : base(
        "Select Allow tools...", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"Project with name {_projectName} does not exists", true);
            return ValueTask.FromResult(false);
        }

        //დადგინდეს ამ ფოლდერებიდან რომელიმე არის თუ არა დასაბექაპებელ სიაში. და თუ არის მისთვის ჩაირთოს ჭეშმარიტი
        Dictionary<string, bool> checks = Enum.GetValues<EProjectTools>().ToDictionary(tool => tool.ToString(),
            tool => project.AllowToolsList.Contains(tool));

        //გამოვიდეს სიიდან ამრჩევი
        MenuInputer.MultipleInputFromList("Select allow tools", checks);

        foreach (KeyValuePair<string, bool> kvp in checks)
        {
            if (!Enum.TryParse(kvp.Key, out EProjectTools tool))
            {
                return ValueTask.FromResult(false);
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

        _parametersManager.Save(parameters, "Changes saved");
        return ValueTask.FromResult(true);
    }
}
