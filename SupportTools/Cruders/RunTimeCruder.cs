using System.Collections.Generic;
using CliMenu;
using LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class RunTimeCruder : SimpleNamesWithDescriptionsCruder
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public RunTimeCruder(IParametersManager parametersManager) : base(parametersManager, "RunTime", "RunTimes")
    {
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)ParametersManager.Parameters).RunTimes;
    }


    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        GenerateStandardRunTimesCliMenuCommand generateCommand = new(ParametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }
}