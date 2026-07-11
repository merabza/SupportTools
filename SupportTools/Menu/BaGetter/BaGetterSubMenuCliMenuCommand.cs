using System;
using System.Collections.Generic;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.ApiClientsManagement;

namespace SupportTools.Menu.BaGetter;

public sealed class BaGetterSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BaGetterSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base("BaGetter", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        var baGetterMenuSet = new CliMenuSet("BaGetter");

        BaGetterApiClient? apiClient = CreateApiClient();
        if (apiClient is null)
        {
            baGetterMenuSet.AddEscapeCommand();
            return baGetterMenuSet;
        }

        List<string>? packageIds = apiClient.GetPackageIds().Result;
        if (packageIds is null)
        {
            baGetterMenuSet.AddEscapeCommand();
            return baGetterMenuSet;
        }

        //სერვერზე არსებული პაკეტების ჩამონათვალი
        foreach (string packageId in packageIds)
        {
            baGetterMenuSet.AddMenuItem(new BaGetterPackageSubMenuCliMenuCommand(_logger, apiClient, packageId));
        }

        baGetterMenuSet.AddEscapeCommand();
        return baGetterMenuSet;
    }

    //BaGetter-ის კლიენტის შექმნა პარამეტრებში მითითებული LocalPackageManagerWebApiClientName-ის მიხედვით
    private BaGetterApiClient? CreateApiClient()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        string? localPackageManagerWebApiClientName = parameters.LocalPackageManagerWebApiClientName;
        if (string.IsNullOrWhiteSpace(localPackageManagerWebApiClientName))
        {
            StShared.WriteErrorLine("LocalPackageManagerWebApiClientName does not specified", true);
            return null;
        }

        ApiClientSettingsDomain apiClientSettings;
        try
        {
            apiClientSettings = parameters.GetApiClientSettingsRequired(localPackageManagerWebApiClientName);
        }
        catch (InvalidOperationException e)
        {
            StShared.WriteException(e, true, _logger);
            return null;
        }

        return new BaGetterApiClient(_logger, _httpClientFactory, apiClientSettings.Server, apiClientSettings.ApiKey);
    }
}
