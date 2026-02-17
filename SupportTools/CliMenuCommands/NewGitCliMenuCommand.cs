using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

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

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Add new Git started");

        var gitCruder = GitCruder.Create(_logger, _httpClientFactory, _parametersManager);
        string? newGitName = gitCruder.GetNameWithPossibleNewName("Git Name", null);

        if (string.IsNullOrWhiteSpace(newGitName))
        {
            StShared.WriteErrorLine("Name is empty", true);
            return ValueTask.FromResult(false);
        }

        //მიმდინარე პარამეტრები
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);

        if (project is null)
        {
            StShared.WriteErrorLine($"project with name {_projectName} does not exists", true);
            return ValueTask.FromResult(false);
        }

        List<string> gitProjectNames = GitProjectNames(project, _gitCol);

        //გადავამოწმოთ ხომ არ არსებობს იგივე სახელით სხვა პროექტი.
        if (gitProjectNames.Contains(newGitName))
        {
            StShared.WriteErrorLine(
                $"Git Project with Name {newGitName} in project {_projectName} is already exists. cannot create new record. ",
                true, _logger);
            return ValueTask.FromResult(false);
        }

        gitProjectNames.Add(newGitName);

        //ცვლილებების შენახვა
        _parametersManager.Save(parameters, "Add Git Project Finished");

        return ValueTask.FromResult(true);
    }

    private List<string> GitProjectNames(ProjectModel project, EGitCol gitCol)
    {
        List<string> gitProjectNames = gitCol switch
        {
            EGitCol.Main => project.GitProjectNames,
            EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
            _ => throw new ArgumentOutOfRangeException(nameof(gitCol), gitCol,
                $"Unsupported git collection type: {gitCol}")
        };
        return gitProjectNames;
    }
}
