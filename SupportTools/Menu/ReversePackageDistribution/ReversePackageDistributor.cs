using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using LibGitData.Domain;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.Models;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ReversePackageDistribution;

//Reverse Package Distribution-ის საერთო ლოგიკა. მთავარი მენიუს, ჯგუფისა და პროექტის ბრძანებები მას თავიანთ პროექტებს
//აწვდიან, remote პროექტებს კი თვითონ ტოვებენ
internal sealed class ReversePackageDistributor
{
    private readonly ILogger _logger;
    private readonly IProjectReferencesEditor _referencesEditor;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ReversePackageDistributor(ILogger logger, IProjectReferencesEditor referencesEditor)
    {
        _logger = logger;
        _referencesEditor = referencesEditor;
    }

    //პროექტები სახელების მიხედვით დალაგებული მუშავდება. false, თუ რომელიმე პროექტის რომელიმე ცვლილება ვერ მოხერხდა
    public bool Run(SupportToolsParameters parameters, IEnumerable<KeyValuePair<string, ProjectModel>> projects,
        CancellationToken cancellationToken)
    {
        var gitProjects = GitProjects.Create(_logger, parameters.GitProjects);

        bool hadErrors = false;
        int projectsCount = 0;

        foreach ((string projectName, ProjectModel project) in projects.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            //ლოკალური პაკეტების რეფერენსები მხოლოდ UseLocalPackages-ჩართულ პროექტებში ბრუნდება
            if (!project.UseLocalPackages)
            {
                StShared.ConsoleWriteInformationLine(_logger, true,
                    $"UseLocalPackages is off for project {projectName}, skipped");
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            projectsCount++;
            hadErrors |= !ProcessProject(parameters, gitProjects, projectName, project);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Reverse package distribution finished. Projects processed: {ProjectsCount}",
                projectsCount);
        }

        return !hadErrors;
    }

    //ერთი პროექტის დამუშავება. ცვლილებები შედის მხოლოდ მთავარ რეპოზიტორიაში
    private bool ProcessProject(SupportToolsParameters parameters, GitProjects gitProjects, string projectName,
        ProjectModel project)
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

                if (_referencesEditor.RemovePackageFromProject(csprojFile, packageId).IsFailure)
                {
                    StShared.WriteErrorLine($"Cannot remove package {packageId} from {csprojFile}", true, _logger);
                    hadErrors = true;
                    continue;
                }

                if (_referencesEditor.AddReferenceToProject(csprojFile, refFullPath).IsFailure)
                {
                    hadErrors = true;

                    //თუ რეფერენსის ჩასმა ვერ მოხერხდა, წაშლილი პაკეტი უბრუნდება პროექტს,
                    //რომ შეცვლილი პროექტი შეცდომაზე არ გავიდეს
                    if (_referencesEditor.AddPackageToProject(csprojFile, packageId, null).IsFailure)
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
