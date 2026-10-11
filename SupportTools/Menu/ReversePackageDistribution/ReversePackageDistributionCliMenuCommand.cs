using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppCliTools.CliMenu;
using LibDotnetWork;
using LibGitData.Domain;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ReversePackageDistribution;

public sealed class ReversePackageDistributionCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Reverse Package Distribution";

    private readonly Func<IProjectReferencesEditor> _createReferencesEditor;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public ReversePackageDistributionCliMenuCommand(ILogger logger, IParametersManager parametersManager) : this(
        logger, parametersManager, () => new DotnetProjectReferencesEditor(new DotnetProcessor(logger, true)))
    {
    }

    //csproj-ის ცვლილებები პარამეტრადაა გამოტანილი, რომ ტესტებმა dotnet არ გაუშვან
    // ReSharper disable once ConvertToPrimaryConstructor
    internal ReversePackageDistributionCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        Func<IProjectReferencesEditor> createReferencesEditor) : base(MenuCommandName, EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _createReferencesEditor = createReferencesEditor;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(
            "This process will replace PackageReferences of own packages with ProjectReferences in main repositories of all projects");
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);
        IProjectReferencesEditor referencesEditor = _createReferencesEditor();

        bool hadErrors = false;
        int projectsCount = 0;

        //remote პროექტების მთავარი რეპოზიტორია ამ კომპიუტერზე არ არის (D3)
        foreach ((string projectName, ProjectModel project) in RemoteOnlyProjects.Skip(parameters.Projects, _logger)
                     .OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            projectsCount++;
            hadErrors |= !ProcessProject(parameters, gitProjects, referencesEditor, projectName, project);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Reverse package distribution finished. Projects processed: {ProjectsCount}",
                projectsCount);
        }

        return ValueTask.FromResult(!hadErrors);
    }

    //ერთი პროექტის დამუშავება. ცვლილებები შედის მხოლოდ მთავარ რეპოზიტორიაში
    private bool ProcessProject(SupportToolsParameters parameters, GitProjects gitProjects,
        IProjectReferencesEditor referencesEditor, string projectName, ProjectModel project)
    {
        if (string.IsNullOrWhiteSpace(project.ProjectFolderName))
        {
            StShared.WriteWarningLine($"ProjectFolderName does not specified for project {projectName}, skipped", true,
                _logger);
            return true;
        }

        var gitRepos = GitRepos.Create(_logger, parameters.Gits, project.SpaProjectFolderRelativePath(gitProjects),
            true, false);

        //პროექტის მთავარი რეპოზიტორია არის ის, რომლის სახელიც ემთხვევა პროექტის სახელს
        if (!project.GitProjectNames.Contains(projectName) ||
            !gitRepos.Gits.TryGetValue(projectName, out GitData? mainGitData))
        {
            StShared.WriteWarningLine($"Main git repository does not found for project {projectName}, skipped", true,
                _logger);
            return true;
        }

        string mainRepoPath =
            Path.GetFullPath(Path.Combine(project.ProjectFolderName, mainGitData.GitProjectFolderName));

        if (!Directory.Exists(mainRepoPath))
        {
            StShared.WriteWarningLine(
                $"Main repository folder {mainRepoPath} does not exists for project {projectName}, skipped", true,
                _logger);
            return true;
        }

        bool hadErrors = false;

        foreach (string csprojFile in Directory.EnumerateFiles(mainRepoPath, "*.csproj", SearchOption.AllDirectories))
        {
            StShared.ConsoleWriteInformationLine(_logger, true, $"Processing {csprojFile}");
            //ჯერ მხოლოდ იკითხება csproj ფაილი, ცვლილებები კეთდება dotnet-ის ბრძანებებით
            XElement projectXml = XElement.Load(csprojFile);
            List<string> packageIds =
            [
                .. projectXml.Descendants("PackageReference").Select(x => (string?)x.Attribute("Include"))
                    .OfType<string>()
            ];

            foreach (string packageId in packageIds)
            {
                //საკუთარი პაკეტია ის, რომელიც GitProjects სიაშია რეგისტრირებული
                GitProjectDataDomain? gitProject = gitProjects.GetGitProjectIfExistsByKey(packageId);
                if (gitProject is null)
                {
                    continue;
                }

                //ProjectRelativePath იწყება გიტის კლონის ფოლდერის სახელით, ამიტომ გზა ეწყობა პროექტის ფოლდერიდან
                string refFullPath = Path.GetFullPath(Path.Combine(project.ProjectFolderName,
                    gitProject.ProjectRelativePath, gitProject.ProjectFileName));

                if (!File.Exists(refFullPath))
                {
                    StShared.WriteWarningLine(
                        $"Project file {refFullPath} does not exists for package {packageId}, package reference in {csprojFile} is not replaced",
                        true, _logger);
                    continue;
                }

                if (referencesEditor.RemovePackageFromProject(csprojFile, packageId).IsFailure)
                {
                    StShared.WriteErrorLine($"Cannot remove package {packageId} from {csprojFile}", true, _logger);
                    hadErrors = true;
                    continue;
                }

                if (referencesEditor.AddReferenceToProject(csprojFile, refFullPath).IsFailure)
                {
                    hadErrors = true;

                    //თუ რეფერენსის ჩასმა ვერ მოხერხდა, წაშლილი პაკეტი უბრუნდება პროექტს,
                    //რომ შეცვლილი პროექტი შეცდომაზე არ გავიდეს
                    if (referencesEditor.AddPackageToProject(csprojFile, packageId, null).IsFailure)
                    {
                        StShared.WriteErrorLine(
                            $"Cannot add reference {refFullPath} to {csprojFile} and cannot restore removed package {packageId}. Recover the file from git",
                            true, _logger);
                        continue;
                    }

                    StShared.WriteErrorLine(
                        $"Cannot add reference {refFullPath} to {csprojFile}. Removed package was restored", true,
                        _logger);
                    continue;
                }

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Replaced package {PackageId} with ProjectReference {ReferencePath} in {ProjectFilePath}",
                        packageId, refFullPath, csprojFile);
                }
            }
        }

        return !hadErrors;
    }
}
