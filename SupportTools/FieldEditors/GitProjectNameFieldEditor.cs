using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

public sealed class GitProjectNameFieldEditor : FieldEditor<string>
{
    private readonly string[] _gitProjectNamesParameterNames;
    private readonly Func<string, CliMenuSet, string?, int> _inputIdFromMenuList;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectExtension;
    private readonly bool _useNone;

    public GitProjectNameFieldEditor(string propertyName, string[] gitProjectNamesParameterNames,
        string projectExtension, IParametersManager parametersManager, bool useNone = false) : this(propertyName,
        gitProjectNamesParameterNames, projectExtension, parametersManager, useNone, MenuInputer.InputIdFromMenuList)
    {
    }

    //კონსოლიდან არჩევა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხები თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal GitProjectNameFieldEditor(string propertyName, string[] gitProjectNamesParameterNames,
        string projectExtension, IParametersManager parametersManager, bool useNone,
        Func<string, CliMenuSet, string?, int> inputIdFromMenuList) : base(propertyName)
    {
        _parametersManager = parametersManager;
        _useNone = useNone;
        _projectExtension = projectExtension;
        _gitProjectNamesParameterNames = gitProjectNamesParameterNames;
        _inputIdFromMenuList = inputIdFromMenuList;
    }

    public override ValueTask UpdateField(string? recordKey, object recordForUpdate,
        CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? currentGitProjectName = GetValue(recordForUpdate);

        List<string> projectGitNames = [];
        foreach (string gitProjectNamesParameterName in _gitProjectNamesParameterNames)
        {
            var pgn = GetValue<List<string>?>(recordForUpdate, gitProjectNamesParameterName);
            if (pgn is not null)
            {
                projectGitNames.AddRange(pgn);
            }
        }

        var gitProjectNamesMenuSet = new CliMenuSet();

        List<string> keys =
        [
            .. parameters.GitProjects
                .Where(x => x.Value.GitName is not null && x.Value.ProjectExtension == _projectExtension &&
                            projectGitNames.Contains(x.Value.GitName)).Select(x => x.Key).OrderBy(x => x)
        ];

        if (keys.Count < 1)
        {
            throw new ListIsEmptyException("GitProjects List is empty");
        }

        if (_useNone)
        {
            gitProjectNamesMenuSet.AddMenuItem("-", new CliMenuCommand("(None)"));
        }

        //ელემენტის id მისი ინდექსია keys-ში. Enter მიმდინარე მნიშვნელობას ტოვებს: სია მისი ელემენტის id-ს აბრუნებს,
        //id-ის გარეშე დამატებულ ელემენტს კი -1 აქვს, რაც (None)-ად ან არასწორ არჩევანად ჩაითვლებოდა
        int id = 0;
        foreach (string listItem in keys)
            //listItem
        {
            gitProjectNamesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(listItem), id++);
        }

        int index = _inputIdFromMenuList(FieldName, gitProjectNamesMenuSet, currentGitProjectName);

        if (_useNone && index == -1)
        {
            SetValue(recordForUpdate, null);
            return ValueTask.CompletedTask;
        }

        if (index < 0 || index >= keys.Count)
        {
            throw new DataInputException("Selected invalid ID. ");
        }

        SetValue(recordForUpdate, keys[index]);
        return ValueTask.CompletedTask;
    }
}
