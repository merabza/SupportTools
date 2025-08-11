﻿using System.Collections.Generic;
using System.Net.Http;
using CliMenu;
using CliParameters.Cruders;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ProjectNpmPackagesLisCruder : SimpleNamesListCruder
{
    private readonly List<string> _currentValuesList;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesListFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public ProjectNpmPackagesLisCruder(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, List<string> currentValuesList) : base("Npm Package",
        "Npm Packages")
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _currentValuesList = currentValuesList;
    }

    //public static ProjectNpmPackagesLisCruder Create(ProjectModel project, IParametersManager parametersManager)
    //{
    //    return new ProjectNpmPackagesLisCruder(parametersManager, project.FrontNpmPackageNames);
    //}

    protected override List<string> GetList()
    {
        return _currentValuesList;
    }

    protected override string? InputNewRecordName()
    {
        var npmPackageCruder = NpmPackagesCruder.Create(_parametersManager);
        var newNpmPackageName = npmPackageCruder.GetNameWithPossibleNewName("Npm Package Name", null);

        if (!string.IsNullOrWhiteSpace(newNpmPackageName))
            return newNpmPackageName;

        StShared.WriteErrorLine("Name is empty", true);
        return null;
    }

    public override string? GetStatusFor(string name)
    {
        var npmPackages = ((SupportToolsParameters)_parametersManager.Parameters).NpmPackages;
        return npmPackages.GetValueOrDefault(name);
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        cruderSubMenuSet.AddMenuItem(new ReCreateUpdateFrontSpaProjectCliMenuCommand(_logger, _httpClientFactory,
            _parametersManager));//_projectName

        ////Check versions for All Tools
        //var checkDotnetToolsVersionsCommand = new CheckDotnetToolsVersionsCliMenuCommand(ParametersManager);
        //cruderSubMenuSet.AddMenuItem(checkDotnetToolsVersionsCommand);

        ////Update All Tools To Latest Version
        //var updateAllToolsToLatestVersionCliMenuCommand =
        //    new UpdateAllToolsToLatestVersionCliMenuCommand(ParametersManager);
        //cruderSubMenuSet.AddMenuItem(updateAllToolsToLatestVersionCliMenuCommand);
    }
}