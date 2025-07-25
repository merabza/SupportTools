using System;
using System.Linq;
using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class NewFrontNpmPackageNameCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    //ახალი პროექტის შექმნის ამოცანა
    // ReSharper disable once ConvertToPrimaryConstructor
    public NewFrontNpmPackageNameCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName)
        : base("Add Npm Package", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override bool RunBody()
    {
        Console.WriteLine("Add new Npm Package started");

        var npmPackageCruder = NpmPackagesCruder.Create(_parametersManager);
        var newGitName = npmPackageCruder.GetNameWithPossibleNewName("Npm Package Name", null);

        if (string.IsNullOrWhiteSpace(newGitName))
        {
            StShared.WriteErrorLine("Name is empty", true);
            return false;
        }

        //მიმდინარე პარამეტრები
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
            return false;
        }

        var npmPackageNames = project.FrontNpmPackageNames;

        //გადავამოწმოთ ხომ არ არსებობს იგივე სახელით სხვა პროექტი.
        if (npmPackageNames.Any(a => a == newGitName))
        {
            StShared.WriteErrorLine(
                $"Npm Package with Name {newGitName} in project {_projectName} is already exists. cannot create new record.",
                true, _logger);
            return false;
        }

        npmPackageNames.Add(newGitName);

        //ცვლილებების შენახვა
        _parametersManager.Save(parameters, "Add Npm Package Finished");

        return true;
    }
}