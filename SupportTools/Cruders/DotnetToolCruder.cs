using System.Collections.Generic;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Tools;
using SupportToolsData.Models;

namespace SupportTools.Cruders;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DotnetToolCruder : ParCruder<DotnetToolData>
{
    private readonly IDotnetToolsRunner _dotnetToolsRunner;

    public DotnetToolCruder(IParametersManager parametersManager,
        Dictionary<string, DotnetToolData> currentValuesDictionary) : this(parametersManager, currentValuesDictionary,
        new DotnetToolsRunner())
    {
    }

    //dotnet tool-ის ბრძანებები პარამეტრადაა გამოტანილი, რომ ტესტებმა ისინი შეცვალონ
    internal DotnetToolCruder(IParametersManager parametersManager,
        Dictionary<string, DotnetToolData> currentValuesDictionary, IDotnetToolsRunner dotnetToolsRunner) : base(
        parametersManager, currentValuesDictionary, "Dotnet Tool", "Dotnet Tools")
    {
        _dotnetToolsRunner = dotnetToolsRunner;
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.PackageId)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.InstalledVersion)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.LatestVersion)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.MaxVersion)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.CommandName)));
        FieldEditors.Add(new TextFieldEditor(nameof(DotnetToolData.Description)));
    }

    //სიაში დაყენებული ვერსიები ჩანს, ამიტომ სიის აგებისას ისინი ახლდება და ცვლილება ინახება: dotnet tool list ქსელს
    //არ იყენებს და სწრაფია. ბოლო ვერსიებს „Check Dotnet Tools Versions...“ ამოწმებს
    protected override void BeforeGetListMenu()
    {
        if (!DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(ParametersManager, _dotnetToolsRunner))
        {
            return;
        }

        //სიის აგება სინქრონულია. შეტყობინება არ იბეჭდება, რადგან მენიუ ეკრანს მაშინვე ასუფთავებს
        ParametersManager.Save(ParametersManager.Parameters, string.Empty).AsTask().GetAwaiter().GetResult();
    }

    public override string? GetStatusFor(string name)
    {
        var dotnetToolData = (DotnetToolData?)GetItemByName(name);
        if (dotnetToolData is null)
        {
            return null;
        }

        //ბოლო ვერსია ჩანს მხოლოდ მაშინ, როცა ცნობილია და დაყენებულისგან განსხვავდება
        string latestVersionStatus = string.IsNullOrWhiteSpace(dotnetToolData.LatestVersion) ||
                                     dotnetToolData.InstalledVersion == dotnetToolData.LatestVersion
            ? string.Empty
            : $"({dotnetToolData.LatestVersion})";
        return $" {dotnetToolData.InstalledVersion} {latestVersionStatus} {dotnetToolData.Description} ";
    }

    protected override void FillListMenuAdditional(CliMenuSet cruderSubMenuSet)
    {
        //Check versions for All Tools
        //ბოლო ვერსიები (ქსელით) ამ ბრძანებით მოწმდება და შედეგი ინახება. სიის აგება მხოლოდ დაყენებულ ვერსიებს ანახლებს
        var checkDotnetToolsVersionsCommand = new CheckDotnetToolsVersionsCliMenuCommand(ParametersManager);
        cruderSubMenuSet.AddMenuItem(checkDotnetToolsVersionsCommand);

        //Update All Tools To Latest Version
        var updateAllToolsToLatestVersionCliMenuCommand =
            new UpdateAllToolsToLatestVersionCliMenuCommand(ParametersManager);
        cruderSubMenuSet.AddMenuItem(updateAllToolsToLatestVersionCliMenuCommand);
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        //Check versions for One Tool
        var checkOneDotnetToolVersionsCliMenuCommand =
            new CheckOneDotnetToolVersionsCliMenuCommand(ParametersManager, itemName);
        itemSubMenuSet.AddMenuItem(checkOneDotnetToolVersionsCliMenuCommand);

        //Update One Tool To Latest Version
        var updateOneToolToLatestVersionCliMenuCommand =
            new UpdateOneToolToLatestVersionCliMenuCommand(ParametersManager, itemName);
        itemSubMenuSet.AddMenuItem(updateOneToolToLatestVersionCliMenuCommand);
    }
}
