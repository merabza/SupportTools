using System.Collections.Generic;
using System.Linq;
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
    private readonly IParametersManager _parametersManager;
    private readonly string _projectExtension;
    private readonly bool _useNone;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectNameFieldEditor(string propertyName, string[] gitProjectNamesParameterNames,
        string projectExtension, IParametersManager parametersManager, bool useNone = false) : base(propertyName)
    {
        _parametersManager = parametersManager;
        _useNone = useNone;
        _projectExtension = projectExtension;
        _gitProjectNamesParameterNames = gitProjectNamesParameterNames;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate)
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

        List<string> keys = parameters.GitProjects
            .Where(x => x.Value.GitName is not null && x.Value.ProjectExtension == _projectExtension &&
                        projectGitNames.Contains(x.Value.GitName)).Select(x => x.Key).OrderBy(x => x).ToList();

        if (keys.Count < 1)
        {
            throw new ListIsEmptyException("GitProjects List is empty");
        }

        if (_useNone)
        {
            gitProjectNamesMenuSet.AddMenuItem("-", new CliMenuCommand("(None)"), 1);
        }

        foreach (string listItem in keys)
            //listItem
        {
            gitProjectNamesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(listItem));
        }

        int index = MenuInputer.InputIdFromMenuList(FieldName, gitProjectNamesMenuSet, currentGitProjectName);

        if (_useNone && index == -1)
        {
            SetValue(recordForUpdate, null);
            return;
        }

        if (index < 0 || index >= keys.Count)
        {
            throw new DataInputException("Selected invalid ID. ");
        }

        SetValue(recordForUpdate, keys[index]);
    }
}
