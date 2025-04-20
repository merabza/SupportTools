using System;
using System.Linq;
using System.Net.Http;
using CliMenu;
using LibGitData;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class NewGitCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    //ახალი პროექტის შექმნის ამოცანა
    // ReSharper disable once ConvertToPrimaryConstructor
    public NewGitCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string projectName, EGitCol gitCol) : base("Add Git Project",
        EMenuAction.Reload)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitCol = gitCol;
    }

    protected override bool RunBody()
    {
        Console.WriteLine("Add new Git started");

        var gitCruder = GitCruder.Create(_logger, _httpClientFactory, _parametersManager);
        var newGitName = gitCruder.GetNameWithPossibleNewName("Git Name", null);

        if (string.IsNullOrWhiteSpace(newGitName))
        {
            StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
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

        var gitProjectNames = _gitCol switch
        {
            EGitCol.Main => project.GitProjectNames,
            EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
            _ => throw new ArgumentOutOfRangeException()
        };

        //გადავამოწმოთ ხომ არ არსებობს იგივე სახელით სხვა პროექტი.
        if (gitProjectNames.Any(a => a == newGitName))
        {
            StShared.WriteErrorLine(
                $"Git Project with Name {newGitName} in project {_projectName} is already exists. cannot create Server with this name. ",
                true, _logger);
            return false;
        }

        gitProjectNames.Add(newGitName);

        //ცვლილებების შენახვა
        _parametersManager.Save(parameters, "Add Git Project Finished");

        return true;
    }
}