using System.Collections.Generic;
using CliParameters.Cruders;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ProjectNpmPackagesLisCruder : SimpleNamesListCruder
{
    private readonly List<string> _currentValuesList;
    private readonly IParametersManager _parametersManager;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesListFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public ProjectNpmPackagesLisCruder(IParametersManager parametersManager, List<string> currentValuesList) : base(
        "Npm Package", "Npm Packages")
    {
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
}