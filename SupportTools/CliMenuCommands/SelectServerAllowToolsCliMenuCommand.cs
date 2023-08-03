﻿using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using LibDataInput;
using LibMenuInput;
using LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class SelectServerAllowToolsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly string _serverName;

    public SelectServerAllowToolsCliMenuCommand(IParametersManager parametersManager, string projectName,
        string serverName)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _serverName = serverName;
    }

    protected override void RunAction()
    {
        try
        {
            //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
            var parameters = (SupportToolsParameters)_parametersManager.Parameters;
            var server = parameters.GetServerByProject(_projectName, _serverName);

            if (server is null)
            {
                StShared.WriteErrorLine($"Server with name {_serverName} does not exists", true);
                return;
            }

            //დადგინდეს ამ ფოლდერებიდან რომელიმე არის თუ არა დასაბექაპებელ სიაში. და თუ არის მისთვის ჩაირთოს ჭეშმარიტი
            var checks =
                ToolCommandFabric.ToolsByProjectsAndServers.ToDictionary(tool => tool.ToString(),
                    tool => server.AllowToolsList?.Contains(tool) ?? false);

            //გამოვიდეს სიიდან ამრჩევი
            MenuInputer.MultipleInputFromList("Select allow tools", checks);


            server.AllowToolsList ??= new List<ETools>();

            foreach (var kvp in checks)
            {
                if (!Enum.TryParse(kvp.Key, out ETools tool))
                    return;

                if (kvp.Value)
                {
                    //ჩართული ჩავამატოთ თუ არ არსებობს
                    if (!server.AllowToolsList.Contains(tool))
                        server.AllowToolsList.Add(tool);
                }
                else
                {
                    //გამორთული ამოვაკლოთ თუ არსებობს
                    if (server.AllowToolsList.Contains(tool))
                        server.AllowToolsList.Remove(tool);
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