﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters.FieldEditors;
using LibGitData.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;

namespace SupportTools.FieldEditors;

public sealed class GitsFieldEditor : FieldEditor<Dictionary<string, GitDataModel>>
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsFieldEditor(ILogger logger, IHttpClientFactory httpClientFactory, string propertyName, ParametersManager parametersManager) : base(
        propertyName, false, null, false, null, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var gitCruder = new GitCruder(_logger, _httpClientFactory, _parametersManager);
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