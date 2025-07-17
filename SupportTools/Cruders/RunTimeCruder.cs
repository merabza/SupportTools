using System.Collections.Generic;
using CliMenu;
using LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class RunTimeCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RunTimeCruder(IParametersManager parametersManager) : base("RunTime", "RunTimes")
    {
        _parametersManager = parametersManager;
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)_parametersManager.Parameters).RunTimes;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        GenerateStandardRunTimesCliMenuCommand generateCommand = new(_parametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }
}