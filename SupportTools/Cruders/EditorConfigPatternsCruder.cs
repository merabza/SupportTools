using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditorConfigPatternsCruder(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, List<string> currentValuesList) : base(parametersManager,
        "EditorConfig Pattern", "EditorConfig Patterns")
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _currentValuesList = currentValuesList;
    }

    public static EditorConfigPatternsCruder Create(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        return new EditorConfigPatternsCruder(logger, httpClientFactory, parametersManager,
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

        var syncCommand = new SyncEditorConfigFilesCliMenuCommand(_logger, _httpClientFactory, _parametersManager);
        cruderSubMenuSet.AddMenuItem(syncCommand);
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
            new ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand(_logger, itemName, _parametersManager);
        itemSubMenuSet.AddMenuItem(applyEditorConfigPatternCliMenuCommand);
    }
}
