using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

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

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Add new Npm Package started");

        var npmPackageCruder = NpmPackagesCruder.Create(_parametersManager);
        string? newGitName =
            await npmPackageCruder.GetNameWithPossibleNewName("Npm Package Name", null, null, false, cancellationToken);

        if (string.IsNullOrWhiteSpace(newGitName))
        {
            StShared.WriteErrorLine("Name is empty", true);
            return false;
        }

        //მიმდინარე პარამეტრები
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
            return false;
        }

        List<string> npmPackageNames = project.FrontNpmPackageNames;

        //გადავამოწმოთ ხომ არ არსებობს იგივე სახელით სხვა პროექტი.
        if (npmPackageNames.Contains(newGitName))
        {
            StShared.WriteErrorLine(
                $"Npm Package with Name {newGitName} in project {_projectName} is already exists. cannot create new record.",
                true, _logger);
            return false;
        }

        npmPackageNames.Add(newGitName);

        //ცვლილებების შენახვა
        await _parametersManager.Save(parameters, "Add Npm Package Finished", null, cancellationToken);

        return true;
    }
}
