using System.Collections.Generic;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class NpmPackagesCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NpmPackagesCruder(IParametersManager parametersManager) : base("Npm Package", "Npm Packages")
    {
        _parametersManager = parametersManager;
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)_parametersManager.Parameters).NpmPackages;
    }
}