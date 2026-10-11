using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibGitData;
using LibGitData.Models;
using LibGitWork.Helpers;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.SyncRegistry;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//„ჩამოიტანე პროექტი აქ“ (D3): remote პროექტის (ProjectFolderName ამ კომპიუტერზე არ არის) ამ კომპიუტერზე ჩამოტანა.
//ნაბიჯები: (ა) ProjectFolderName-ის შექმნა; (გ) GitProjects-ის ჩანაწერები იმ git-ებისთვის, რომლებსაც ისინი აკლია
//(Update Git Projects-ის სკანირება, მომხმარებლის არჩევანი). ეს ნაბიჯი კლონამდე სრულდება, რომ SpaProjectName-ის
//ჩანაწერით {SpaProjectFolderRelativePath}-იანი git-ების ფოლდერიც დადგინდეს; (ბ) git-ების კლონი V2 მექანიზმით
//(ScaffoldSeeder-ის git-ები ცალკე კითხვით); (დ) შაბლონების ფაილების შემოწმება; (ე) საიდუმლო ფაილების (C6)
//შემოწმება და, თუ აკლია, „Sync Registry“-ის შეთავაზება. ბოლოს შედეგის ანგარიში
public sealed class CloneProjectToThisComputerCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Clone Project To This Computer";

    private readonly IProjectCloneGitActions _gitActions;
    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly Func<CancellationToken, ValueTask> _runSyncRegistry;

    public CloneProjectToThisComputerCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, string projectName) : this(logger, parametersManager, projectName,
        new ProjectCloneGitActions(logger, parametersManager),
        (fieldName, defaultValue) => Inputer.InputBool(fieldName, defaultValue, false),
        cancellationToken =>
            new SyncRegistryCliMenuCommand(logger, httpClientFactory, parametersManager).Run(cancellationToken))
    {
    }

    //git-ის ნაბიჯები, კონსოლიდან შეყვანა და Sync Registry პარამეტრებადაა გამოტანილი, რომ ტესტებმა ისინი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal CloneProjectToThisComputerCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        string projectName, IProjectCloneGitActions gitActions, Func<string, bool, bool> inputBool,
        Func<CancellationToken, ValueTask> runSyncRegistry) : base(MenuCommandName, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitActions = gitActions;
        _inputBool = inputBool;
        _runSyncRegistry = runSyncRegistry;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} does not exist", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(project.ProjectFolderName))
        {
            StShared.WriteErrorLine($"ProjectFolderName is not specified for project {_projectName}", true, _logger);
            return false;
        }

        string projectFolderName = project.ProjectFolderName;
        if (ProjectPresence.IsLocal(project))
        {
            Console.WriteLine($"Project {_projectName} is already on this computer: {projectFolderName}");
            return true;
        }

        List<(EGitCol GitCol, string GitName)> gits = ChooseGits(project);
        if (!_inputBool(
                $"Clone {Count(gits.Count)} gits of project {_projectName} to {projectFolderName}?", true))
        {
            return true;
        }

        //(ა) პროექტის ფოლდერი
        if (!CreateFolder(projectFolderName))
        {
            return false;
        }

        List<string> report = [$"Folder {projectFolderName} created"];
        bool success = await PrepareGitProjects(parameters, project, gits, report, cancellationToken);
        Dictionary<string, string?> gitFolders = CloneGits(parameters, project, gits);
        success &= ReportClonedGits(gits, gitFolders, report);

        //(დ) შაბლონების ფაილები
        List<string> templateProblems = ProjectCloneChecks.GetTemplateProblems(parameters, project, gitFolders);
        report.AddRange(templateProblems.Count == 0 ? ["Templates: OK"] : templateProblems);

        //(ე) საიდუმლო ფაილები
        await CheckStoredFiles(project, report, cancellationToken);

        WriteReport(report, success);
        return success;
    }

    private List<(EGitCol GitCol, string GitName)> ChooseGits(ProjectModel project)
    {
        List<(EGitCol GitCol, string GitName)> gits = [.. project.GitProjectNames.Select(x => (EGitCol.Main, x))];

        int scaffoldSeederGitsCount = project.ScaffoldSeederGitProjectNames.Count;
        if (scaffoldSeederGitsCount > 0 &&
            _inputBool($"Clone also {Count(scaffoldSeederGitsCount)} ScaffoldSeeder gits of project {_projectName}?",
                false))
        {
            gits.AddRange(project.ScaffoldSeederGitProjectNames.Select(x => (EGitCol.ScaffoldSeed, x)));
        }

        return gits;
    }

    private bool CreateFolder(string projectFolderName)
    {
        try
        {
            Directory.CreateDirectory(projectFolderName);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
                                      or NotSupportedException)
        {
            StShared.WriteErrorLine($"Folder {projectFolderName} cannot be created: {e.Message}", true, _logger);
            return false;
        }
    }

    //(გ) git-ები, რომელთა csproj/esproj ჩანაწერები GitProjects-ში აკლია, სკანირდება და პარამეტრები ინახება
    private async ValueTask<bool> PrepareGitProjects(SupportToolsParameters parameters, ProjectModel project,
        List<(EGitCol GitCol, string GitName)> gits, List<string> report, CancellationToken cancellationToken)
    {
        bool success = true;
        List<string> gitNamesToScan = ProjectCloneChecks.GetGitsWithoutGitProjects(parameters,
            gits.Select(x => x.GitName));
        if (gitNamesToScan.Count > 0)
        {
            List<string> scannedGitNames = [];
            foreach (string gitName in gitNamesToScan)
            {
                if (_gitActions.ScanGitProjects(gitName))
                {
                    scannedGitNames.Add(gitName);
                }
                else
                {
                    report.Add($"Update Git Projects failed for git {gitName}");
                    success = false;
                }
            }

            if (scannedGitNames.Count > 0)
            {
                await _parametersManager.Save(parameters, "Git Projects Updated", null, cancellationToken);
                report.Add($"GitProjects updated for gits: {string.Join(", ", scannedGitNames)}");
            }
        }

        if (!project.IsSpaProjectMissing(GitProjects.Create(null, parameters.GitProjects)))
        {
            return success;
        }

        report.Add(SpaProjectFolderHelper.GetMissingSpaProjectMessage(_projectName, project.SpaProjectName));
        return false;
    }

    //(ბ) git-ების კლონი. ბრუნდება git-ების ფოლდერები (null - ფოლდერი ვერ დგინდება)
    private Dictionary<string, string?> CloneGits(SupportToolsParameters parameters, ProjectModel project,
        List<(EGitCol GitCol, string GitName)> gits)
    {
        foreach ((EGitCol gitCol, string gitName) in gits)
        {
            _gitActions.SyncGit(_projectName, gitCol, gitName);
        }

        Dictionary<string, string?> gitFolders = new(StringComparer.OrdinalIgnoreCase);
        foreach (IGrouping<EGitCol, (EGitCol GitCol, string GitName)> gitColGits in gits.GroupBy(x => x.GitCol))
        {
            string? gitsFolder = gitColGits.Key == EGitCol.Main
                ? project.ProjectFolderName
                : parameters.GetGitsFolder(_projectName, gitColGits.Key);
            foreach ((string gitName, string? gitFolder) in ProjectCloneChecks.GetGitFolders(parameters,
                         _projectName, project, gitsFolder, gitColGits.Select(x => x.GitName)))
            {
                gitFolders[gitName] = gitFolder;
            }
        }

        return gitFolders;
    }

    //დაკლონილად ითვლება git, რომლის ფოლდერიც სინქრონიზაციის შემდეგ არსებობს
    private static bool ReportClonedGits(List<(EGitCol GitCol, string GitName)> gits,
        Dictionary<string, string?> gitFolders, List<string> report)
    {
        List<string> notClonedGitNames =
        [
            .. gits.Select(x => x.GitName).Where(x =>
                gitFolders.GetValueOrDefault(x) is not { } gitFolder || !Directory.Exists(gitFolder))
        ];
        report.Add(
            $"Gits cloned: {Count(gits.Count - notClonedGitNames.Count)} of {Count(gits.Count)}");
        if (notClonedGitNames.Count == 0)
        {
            return true;
        }

        report.Add($"Gits not cloned: {string.Join(", ", notClonedGitNames)}");
        return false;
    }

    private async ValueTask CheckStoredFiles(ProjectModel project, List<string> report,
        CancellationToken cancellationToken)
    {
        List<string> missingFiles = ProjectCloneChecks.GetMissingStoredFiles(project);
        if (missingFiles.Count > 0)
        {
            foreach (string missingFile in missingFiles)
            {
                Console.WriteLine($"Missing file: {missingFile}");
            }

            if (_inputBool(
                    $"{Count(missingFiles.Count)} files of the project are missing. Run \"{SyncRegistryCliMenuCommand.CommandName}\" now?",
                    true))
            {
                await _runSyncRegistry(cancellationToken);
                missingFiles = ProjectCloneChecks.GetMissingStoredFiles(project);
            }
        }

        report.Add(missingFiles.Count == 0
            ? "Project files: OK"
            : $"Project files missing (run \"{SyncRegistryCliMenuCommand.CommandName}\"): {string.Join(", ", missingFiles)}");
    }

    private void WriteReport(List<string> report, bool success)
    {
        Console.WriteLine();
        Console.WriteLine($"{MenuCommandName} {_projectName}: {(success ? "finished" : "finished with errors")}");
        foreach (string line in report)
        {
            Console.WriteLine($"  {line}");
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("{MenuCommandName} {ProjectName} finished, success: {Success}", MenuCommandName,
                _projectName, success);
        }
    }

    private static string Count(int count)
    {
        return count.ToString(CultureInfo.InvariantCulture);
    }
}
