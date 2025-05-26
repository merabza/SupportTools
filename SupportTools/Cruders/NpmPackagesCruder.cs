using System.Collections.Generic;
using LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class NpmPackagesCruder : SimpleNamesWithDescriptionsCruder
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public NpmPackagesCruder(IParametersManager parametersManager) : base(parametersManager, "Npm Package",
        "Npm Packages")
    {
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)ParametersManager.Parameters).NpmPackages;
    }
}