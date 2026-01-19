using System.Collections.Generic;
using CliMenu;
using CliParameters.Cruders;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class RunTimeCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly Dictionary<string, string> _currentValuesDict;
    private readonly IParametersManager _parametersManager;

    //public კონსტრუქტორი საჭიროა. გამოიყენება რეფლექსიით SimpleNamesWithDescriptionsFieldEditor-ში
    // ReSharper disable once ConvertToPrimaryConstructor
    // ReSharper disable once MemberCanBePrivate.Global
    public RunTimeCruder(IParametersManager parametersManager, Dictionary<string, string> currentValuesDict) : base(
        "RunTime", "RunTimes")
    {
        _parametersManager = parametersManager;
        _currentValuesDict = currentValuesDict;
    }

    public static RunTimeCruder Create(IParametersManager parametersManager)
    {
        return new RunTimeCruder(parametersManager, ((SupportToolsParameters)parametersManager.Parameters).RunTimes);
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return _currentValuesDict;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var generateCommand = new GenerateStandardRunTimesCliMenuCommand(_parametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }
}