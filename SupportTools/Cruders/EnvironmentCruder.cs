using System.Collections.Generic;
using CliMenu;
using LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

public sealed class EnvironmentCruder : SimpleNamesWithDescriptionsCruder
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EnvironmentCruder(IParametersManager parametersManager) : base("Environment", "Environments")
    {
        _parametersManager = parametersManager;
    }

    protected override Dictionary<string, string> GetDictionary()
    {
        return ((SupportToolsParameters)_parametersManager.Parameters).Environments;
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        var generateCommand = new GenerateStandardEnvironmentsCliMenuCommand(_parametersManager);
        cruderSubMenuSet.AddMenuItem(generateCommand);
    }
}