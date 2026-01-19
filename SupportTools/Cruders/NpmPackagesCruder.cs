using System.Collections.Generic;
using CliParameters.Cruders;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class NpmPackagesCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly Dictionary<string, string> _currentValuesDict;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesWithDescriptionsFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public NpmPackagesCruder(Dictionary<string, string> currentValuesDict) : base("Npm Package", "Npm Packages")
    {
        _currentValuesDict = currentValuesDict;
    }

    public static NpmPackagesCruder Create(IParametersManager parametersManager)
    {
        return new NpmPackagesCruder(((SupportToolsParameters)parametersManager.Parameters).NpmPackages);
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return _currentValuesDict;
    }
}