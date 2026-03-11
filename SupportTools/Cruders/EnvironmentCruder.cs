using System.Collections.Generic;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Cruders;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class EnvironmentCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly Dictionary<string, string> _currentValuesDict;
    private readonly IParametersManager _parametersManager;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesWithDescriptionsFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public EnvironmentCruder(IParametersManager parametersManager, Dictionary<string, string> currentValuesDict) : base(
        "Environment", "Environments")
    {
        _parametersManager = parametersManager;
        _currentValuesDict = currentValuesDict;
    }

    public static EnvironmentCruder Create(IParametersManager parametersManager)
    {
        return new EnvironmentCruder(parametersManager,
            ((SupportToolsParameters)parametersManager.Parameters).Environments);
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return _currentValuesDict;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var generateCommand = new GenerateStandardEnvironmentsCliMenuCommand(_parametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }
}
