using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Cruders;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class EditorConfigPatternsCruder : SimpleNamesListCruder
{
    private readonly List<string> _currentValuesList;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditorConfigPatternsCruder(ILogger logger, IParametersManager parametersManager,
        List<string> currentValuesList) : base("EditorConfig Pattern", "EditorConfig Patterns")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _currentValuesList = currentValuesList;
    }

    public static EditorConfigPatternsCruder Create(ILogger logger, IParametersManager parametersManager)
    {
        return new EditorConfigPatternsCruder(logger, parametersManager,
            ((SupportToolsParameters)parametersManager.Parameters).EditorConfigPatterns);
    }

    protected override List<string> GetList()
    {
        return _currentValuesList;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var checkEditorConfigFilesCliMenuCommand =
            new CheckEditorConfigFilesCliMenuCommand(_logger, _parametersManager);
        cruderSubMenuSet.AddMenuItem(checkEditorConfigFilesCliMenuCommand);

        var updateEditorConfigFilesCliMenuCommand =
            new UpdateEditorConfigFilesCliMenuCommand(_logger, _parametersManager);
        cruderSubMenuSet.AddMenuItem(updateEditorConfigFilesCliMenuCommand);
    }

    public override string GetStatusFor(string name)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        int usageCount =
            supportToolsParameters.Projects.Values.Count(project => project.EditorConfigPatternName == name);

        return $"Usage count is: {usageCount}";
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        var applyEditorConfigPatternCliMenuCommand =
            new ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand(_logger, itemName,
                _parametersManager);
        itemSubMenuSet.AddMenuItem(applyEditorConfigPatternCliMenuCommand);
    }
}
