﻿using System.Collections.Generic;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ProjectNpmPackagesLisCruder : SimpleNamesListCruder
{
    private readonly ProjectModel _project;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectNpmPackagesLisCruder(IParametersManager parametersManager, ProjectModel project) : base(
        parametersManager, "Npm Package", "Npm Packages")
    {
        _project = project;
    }

    protected override List<string> GetList()
    {
        return _project.FrontNpmPackageNames;
    }

    protected override string? InputNewRecordName()
    {
        var npmPackageCruder = new NpmPackagesCruder(ParametersManager);
        var newNpmPackageName = npmPackageCruder.GetNameWithPossibleNewName("Npm Package Name", null);

        if (!string.IsNullOrWhiteSpace(newNpmPackageName))
            return newNpmPackageName;

        StShared.WriteErrorLine("Name is empty", true);
        return null;
    }

    public override string? GetStatusFor(string name)
    {
        var npmPackages = ((SupportToolsParameters)ParametersManager.Parameters).NpmPackages;
        return npmPackages.GetValueOrDefault(name);
    }
}