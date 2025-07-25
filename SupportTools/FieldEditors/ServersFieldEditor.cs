﻿//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using CliMenu;
//using CliParameters.FieldEditors;
//using LibParameters;
//using Microsoft.Extensions.Logging;
//using SupportTools.Cruders;
//using SupportToolsData.Models;

//namespace SupportTools.FieldEditors;

//public sealed class ServersFieldEditor : FieldEditor<Dictionary<string, ServerDataModel>>
//{
//    private readonly IHttpClientFactory _httpClientFactory;
//    private readonly ILogger _logger;
//    private readonly ParametersManager _parametersManager;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public ServersFieldEditor(string propertyName, ILogger logger, IHttpClientFactory httpClientFactory,
//        ParametersManager parametersManager) : base(propertyName, false, null, true, null, true)
//    {
//        _logger = logger;
//        _httpClientFactory = httpClientFactory;
//        _parametersManager = parametersManager;
//    }

//    public override CliMenuSet GetSubMenu(object record)
//    {
//        var serverDataCruder = ServerDataCruder.Create(_logger, _httpClientFactory, _parametersManager);
//        var menuSet = serverDataCruder.GetListMenu();
//        return menuSet;
//    }

//    public override string GetValueStatus(object? record)
//    {
//        var val = GetValue(record);

//        if (val is not { Count: > 0 })
//            return "No Details";

//        if (val.Count > 1)
//            return $"{val.Count} Details";

//        var kvp = val.Single();
//        return kvp.Key;
//    }
//}

