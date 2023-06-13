﻿using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

public sealed class GitsFieldEditor : FieldEditor<Dictionary<string, GitDataModel>>
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    public GitsFieldEditor(ILogger logger, string propertyName, ParametersManager parametersManager) : base(
        propertyName, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var gitCruder = new GitCruder(_logger, _parametersManager);
        var menuSet = gitCruder.GetListMenu();
        return menuSet;
    }


    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val is not { Count: > 0 })
            return "No Details";

        if (val.Count > 1)
            return $"{val.Count} Details";

        var kvp = val.Single();
        return kvp.Key;
    }
}