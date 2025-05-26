using System.Collections.Generic;
using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class ReactAppTypeCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReactAppTypeCruder(ILogger logger, IParametersManager parametersManager) : base(parametersManager,
        "React App Type", "React App Types")
    {
        _logger = logger;
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)ParametersManager.Parameters).ReactAppTemplates;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        ReCreateAllReactAppsByTemplatesCliMenuCommand reCreateAllReactAppsByTemplatesCliMenuCommand =
            new(_logger, ParametersManager);
        cruderSubMenuSet.AddMenuItem(reCreateAllReactAppsByTemplatesCliMenuCommand);
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        var reactAppTemplateNames = GetDictionary();
        if (!reactAppTemplateNames.TryGetValue(recordKey, out var name))
            return;

        ReCreateReactAppByTemplateNameCliMenuCommand reCreateReactCommand =
            new(_logger, ParametersManager, recordKey, name);
        itemSubMenuSet.AddMenuItem(reCreateReactCommand);
    }
}