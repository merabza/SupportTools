using System.Collections.Generic;
using CliMenu;
using LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class EnvironmentCruder : SimpleNamesWithDescriptionsCruder
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public EnvironmentCruder(IParametersManager parametersManager) : base(parametersManager, "Environment",
        "Environments")
    {
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)ParametersManager.Parameters).Environments;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        GenerateStandardEnvironmentsCliMenuCommand generateCommand = new(ParametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }
}