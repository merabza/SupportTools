using System;
using System.Collections.Generic;
using System.Linq;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace SupportTools.FieldEditors;

public sealed class ProjectSimpleNamesListFieldEditor<T> : FieldEditor<List<string>> where T : Cruder
{
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSimpleNamesListFieldEditor(string propertyName, ParametersManager parametersManager) : base(
        propertyName, false, null, false, null, true)
    {
        _parametersManager = parametersManager;
    }

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSimpleNamesListFieldEditor(ILogger logger, string propertyName,
        ParametersManager parametersManager) : base(propertyName, false, null, false, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var project = record as ProjectModel;
        var gitCruder = _logger is null
            ? (T)Activator.CreateInstance(typeof(T), _parametersManager, project)!
            : (T)Activator.CreateInstance(typeof(T), _logger, _parametersManager, project)!;
        return gitCruder.GetListMenu();
    }

    public override string GetValueStatus(object? record)
    {
        var val = GetValue(record);

        if (val is not { Count: > 0 })
            return "No Details";

        if (val.Count > 1)
            return $"{val.Count} Details";

        var element = val.Single();
        return element;
    }
}