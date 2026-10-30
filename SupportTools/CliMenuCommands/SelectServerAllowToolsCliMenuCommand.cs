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

public sealed class SelectServerAllowToolsCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly Action<string, Dictionary<string, bool>> _selectTools;
    private readonly string _serverName;

    public SelectServerAllowToolsCliMenuCommand(IParametersManager parametersManager, string projectName,
        string serverName) : this(parametersManager, projectName, serverName,
        (caption, checks) => MenuInputer.MultipleInputFromList(caption, checks))
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა არჩევანი თვითონ მიაწოდონ (checks-ს ადგილზე ცვლის)
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SelectServerAllowToolsCliMenuCommand(IParametersManager parametersManager, string projectName,
        string serverName, Action<string, Dictionary<string, bool>> selectTools) : base("Select Allow tools...",
        EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _projectName = projectName;
        _serverName = serverName;
        _selectTools = selectTools;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //პროექტისა და სერვერისათვის შესაძლო ამოცანების ჩამონათვალი (გაშვების შესაძლებლობა)
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        ServerInfoModel? server = parameters.GetServerByProject(_projectName, _serverName);

        if (project is null || server is null)
        {
            StShared.WriteErrorLine($"Server with name {_serverName} is not exists 2", true);
            return false;
        }

        //დადგინდეს ამ ფოლდერებიდან რომელიმე არის თუ არა დასაბექაპებელ სიაში. და თუ არის მისთვის ჩაირთოს ჭეშმარიტი
        //ჩანს მხოლოდ პროექტის ტიპისა და UsesDatabase-ის მიხედვით ხილული ინსტრუმენტები. დამალულის არჩევა არ იცვლება
        Dictionary<string, bool> checks = Enum.GetValues<EProjectServerTools>().Where(project.IsToolVisible)
            .ToDictionary(tool => tool.ToString(), tool => server.AllowToolsList?.Contains(tool) ?? false);

        //გამოვიდეს სიიდან ამრჩევი
        _selectTools("Select allow tools", checks);

        server.AllowToolsList ??= [];

        foreach (KeyValuePair<string, bool> kvp in checks)
        {
            if (!Enum.TryParse(kvp.Key, out EProjectServerTools tool))
            {
                return false;
            }

            if (kvp.Value)
            {
                //ჩართული ჩავამატოთ თუ არ არსებობს
                if (!server.AllowToolsList.Contains(tool))
                {
                    server.AllowToolsList.Add(tool);
                }
            }
            else
            {
                //გამორთული ამოვაკლოთ თუ არსებობს
                server.AllowToolsList.Remove(tool);
            }
        }

        await _parametersManager.Save(parameters, "Changes saved", null, cancellationToken);
        return true;
    }
}
