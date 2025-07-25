using System.Collections.Generic;
using CliMenu;
using CliParameters.Cruders;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ReactAppTypeCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly Dictionary<string, string> _currentValuesDict;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesWithDescriptionsFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public ReactAppTypeCruder(ILogger logger, IParametersManager parametersManager,
        Dictionary<string, string> currentValuesDict) : base("React App Type", "React App Types")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _currentValuesDict = currentValuesDict;
    }

    public static ReactAppTypeCruder Create(ILogger logger, IParametersManager parametersManager)
    {
        return new ReactAppTypeCruder(logger, parametersManager,
            ((SupportToolsParameters)parametersManager.Parameters).ReactAppTemplates);
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return _currentValuesDict;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var reCreateAllReactAppsByTemplatesCliMenuCommand =
            new ReCreateAllReactAppsByTemplatesCliMenuCommand(_logger, _parametersManager);
        cruderSubMenuSet.AddMenuItem(reCreateAllReactAppsByTemplatesCliMenuCommand);
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        var reactAppTemplateNames = GetDictionary();
        if (!reactAppTemplateNames.TryGetValue(recordKey, out var name))
            return;

        var reCreateReactCommand =
            new ReCreateReactAppByTemplateNameCliMenuCommand(_logger, _parametersManager, recordKey, name);
        itemSubMenuSet.AddMenuItem(reCreateReactCommand);
    }
}