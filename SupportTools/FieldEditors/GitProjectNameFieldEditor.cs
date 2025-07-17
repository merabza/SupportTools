using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters.CliMenuCommands;
using CliParameters.FieldEditors;
using LibDataInput;
using LibMenuInput;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

public sealed class GitProjectNameFieldEditor : FieldEditor<string>
{
    private readonly string _gitProjectNamesParameterName;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectExtension;
    private readonly bool _useNone;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectNameFieldEditor(string propertyName, string gitProjectNamesParameterName, string projectExtension,
        IParametersManager parametersManager, bool useNone = false) : base(propertyName)
    {
        _parametersManager = parametersManager;
        _useNone = useNone;
        _projectExtension = projectExtension;
        _gitProjectNamesParameterName = gitProjectNamesParameterName;
    }

    public override void UpdateField(string? recordKey, object recordForUpdate)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var currentGitProjectName = GetValue(recordForUpdate);

        var projectGitNames = GetValue<List<string>?>(recordForUpdate, _gitProjectNamesParameterName);

        var gitProjectNamesMenuSet = new CliMenuSet();

        var keys = parameters.GitProjects
            .Where(x => projectGitNames is not null && x.Value.GitName is not null &&
                        x.Value.ProjectExtension == _projectExtension && projectGitNames.Contains(x.Value.GitName))
            .Select(x => x.Key).OrderBy(x => x).ToList();

        if (keys.Count < 1)
            throw new ListIsEmptyException("GitProjects List is empty");

        if (_useNone) gitProjectNamesMenuSet.AddMenuItem("-", new CliMenuCommand("(None)"), 1);

        foreach (var listItem in keys)
            //listItem
            gitProjectNamesMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(listItem));

        var index = MenuInputer.InputIdFromMenuList(FieldName, gitProjectNamesMenuSet, currentGitProjectName);

        if (_useNone && index == -1)
        {
            SetValue(recordForUpdate, null);
            return;
        }

        if (index < 0 || index >= keys.Count)
            throw new DataInputException("Selected invalid ID. ");

        SetValue(recordForUpdate, keys[index]);
    }
}