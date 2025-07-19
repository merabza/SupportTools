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
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReactAppTypeCruder(ILogger logger, IParametersManager parametersManager) : base("React App Type",
        "React App Types")
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)_parametersManager.Parameters).ReactAppTemplates;
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