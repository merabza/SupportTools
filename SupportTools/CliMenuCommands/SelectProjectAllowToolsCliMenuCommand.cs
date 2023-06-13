using System;
using System.Linq;
using CliMenu;
using LibDataInput;
using LibMenuInput;
using LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class SelectProjectAllowToolsCliMenuCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    public SelectProjectAllowToolsCliMenuCommand(ParametersManager parametersManager, string projectName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override void RunAction()
    {
        try
        {
            //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;

            var project = parameters.GetProject(_projectName);

            if (project is null)
            {
                StShared.WriteErrorLine($"Project with name {_projectName} does not exists", true);
                return;
            }

            //დადგინდეს ამ ფოლდერებიდან რომელიმე არის თუ არა დასაბექაპებელ სიაში. და თუ არის მისთვის ჩაირთოს ჭეშმარიტი
            var checks = ToolCommandFabric.ToolsByProjects.ToDictionary(tool => tool.ToString(),
                tool => project.AllowToolsList.Contains(tool));

            //გამოვიდეს სიიდან ამრჩევი
            MenuInputer.MultipleInputFromList("Select allow tools", checks);

            foreach (var kvp in checks)
            {
                if (!Enum.TryParse(kvp.Key, out ETools tool))
                    return;

                if (kvp.Value)
                {
                    //ჩართული ჩავამატოთ თუ არ არსებობს
                    if (!project.AllowToolsList.Contains(tool))
                        project.AllowToolsList.Add(tool);
                }
                else
                {
                    //გამორთული ამოვაკლოთ თუ არსებობს
                    if (project.AllowToolsList.Contains(tool))
                        project.AllowToolsList.Remove(tool);
                }
            }

            _parametersManager.Save(parameters, "Changes saved");
            MenuAction = EMenuAction.Reload;
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            MenuAction = EMenuAction.Reload;
            StShared.Pause();
        }

        catch (Exception e)
        {
            StShared.WriteException(e, true);
            throw;
        }
    }
}