using System.Collections.Generic;
using CliMenu;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DotnetToolCruder : ParCruder<DotnetToolData>
{
    public DotnetToolCruder(IParametersManager parametersManager,
        Dictionary<string, DotnetToolData> currentValuesDictionary) : base(parametersManager, currentValuesDictionary,
        "Dotnet Tool", "Dotnet Tools")
    {
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.PackageId)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.InstalledVersion)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.LatestVersion)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.CommandName)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.Description)));
    }

    public override string? GetStatusFor(string name)
    {
        var dotnetToolData = (DotnetToolData?)GetItemByName(name);
        return dotnetToolData is null
            ? null
            : $" {dotnetToolData.InstalledVersion} {(dotnetToolData.InstalledVersion == dotnetToolData.LatestVersion ? string.Empty : $"({dotnetToolData.LatestVersion})")} {dotnetToolData.Description} ";
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        //Check versions for All Tools
        var checkDotnetToolsVersionsCommand = new CheckDotnetToolsVersionsCliMenuCommand(ParametersManager);
        cruderSubMenuSet.AddMenuItem(checkDotnetToolsVersionsCommand);

        //Update All Tools To Latest Version
        var updateAllToolsToLatestVersionCliMenuCommand =
            new UpdateAllToolsToLatestVersionCliMenuCommand(ParametersManager);
        cruderSubMenuSet.AddMenuItem(updateAllToolsToLatestVersionCliMenuCommand);
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string recordKey)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, recordKey);

        //Check versions for One Tool
        var checkOneDotnetToolVersionsCliMenuCommand = new CheckOneDotnetToolVersionsCliMenuCommand(ParametersManager, recordKey);
        itemSubMenuSet.AddMenuItem(checkOneDotnetToolVersionsCliMenuCommand);

        //Update One Tool To Latest Version
        var updateOneToolToLatestVersionCliMenuCommand =
            new UpdateOneToolToLatestVersionCliMenuCommand(ParametersManager, recordKey);
        itemSubMenuSet.AddMenuItem(updateOneToolToLatestVersionCliMenuCommand);
    }
}