using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using LibGitData.Models;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Menu;

//"Check Solutions" ბრძანებების საერთო ლოგიკა: პროექტების სოლუშენების შემოწმება და შედეგების მეხსიერებაში ჩაწერა.
//მოწმდება სოლუშენის ფოლდერში მდებარე (სოლუშენის საკუთარი) პროექტების პირდაპირი ProjectReference-ები. სოლუშენის სიაში
//ჩაწერილი მეზობელი პროექტები არ ითვლება: ისინი იმისთვისაა შენახული, რომ პაკეტიდან პროექტზე დაბრუნება სოლუშენის
//პროექტებზე მოხდეს
public static class ProjectSolutionChecker
{
    public static void CheckProjects(SupportToolsParameters parameters,
        IEnumerable<KeyValuePair<string, ProjectModel>> projects, SupportToolsMenuParameters menuParameters,
        CancellationToken cancellationToken)
    {
        foreach ((string projectName, ProjectModel project) in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectSolutionCheckResult? result = CheckProject(parameters, project);
            if (result is null)
            {
                //სოლუშენის გარეშე პროექტს (მაგალითად, არა-.NET პროექტს) სოლუშენის სტატუსი არ აქვს
                menuParameters.ProjectSolutionCheckResults.Remove(projectName);
                Console.WriteLine($"{projectName}: solution file name is not specified, skipped");
                continue;
            }

            menuParameters.ProjectSolutionCheckResults[projectName] = result;
            ProjectSolutionCheckStatusView.WriteResultLine(projectName, result);
        }
    }

    //null, თუ პროექტს სოლუშენის ფაილის სახელი არ აქვს
    public static ProjectSolutionCheckResult? CheckProject(SupportToolsParameters parameters, ProjectModel project)
    {
        string? solutionFileName = project.SolutionFileName;
        if (string.IsNullOrWhiteSpace(solutionFileName))
        {
            return null;
        }

        if (!File.Exists(solutionFileName))
        {
            return NeedsAttention($"solution file {solutionFileName} does not exist");
        }

        if (!SolutionFileReader.IsSolutionFile(solutionFileName))
        {
            return NeedsAttention($"{solutionFileName} is not a solution file");
        }

        List<string> projectPaths;
        try
        {
            projectPaths = SolutionFileReader.ReadProjectPaths(solutionFileName);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return NeedsAttention($"cannot read {solutionFileName}: {e.Message}");
        }

        //არსებული ფაილის სრულ გზას ფოლდერი ყოველთვის აქვს
        var classifier = new ReferencesClassifier(parameters, project,
            Path.GetDirectoryName(Path.GetFullPath(solutionFileName))!);
        foreach (string projectPath in projectPaths)
        {
            classifier.AddSolutionProject(projectPath);
        }

        return classifier.CreateResult();
    }

    private static ProjectSolutionCheckResult NeedsAttention(string problem)
    {
        return new ProjectSolutionCheckResult(EProjectSolutionCheckStatus.NeedsAttention, [problem]);
    }

    //ერთი სოლუშენის რეფერენსების შეფასება. S სოლუშენის ფოლდერია, P - მისი მშობელი. რეფერენსი S-ში რიგზეა; P-ში
    //ფასდება Distribute all packages-ის (PackageDistributor-ის) პირობებით; P-ის გარეთ (შორი) - Needs attention
    private sealed class ReferencesClassifier
    {
        private readonly List<string> _packagesToDistribute = [];
        private readonly SupportToolsParameters _parameters;
        private readonly string? _parentFolder;
        private readonly List<string> _problems = [];
        private readonly ProjectModel _project;
        private readonly string _solutionFolder;

        // ReSharper disable once ConvertToPrimaryConstructor
        public ReferencesClassifier(SupportToolsParameters parameters, ProjectModel project, string solutionFolder)
        {
            _parameters = parameters;
            _project = project;
            _solutionFolder = solutionFolder;
            //დისკის ძირში მდებარე სოლუშენს მშობელი ფოლდერი არ აქვს: მისი ყველა გარე რეფერენსი შორია
            _parentFolder = Path.GetDirectoryName(solutionFolder);
        }

        //სოლუშენის სიის ერთი პროექტი. სიაში ჩაწერილი მეზობელი პროექტები არ მოწმდება
        public void AddSolutionProject(string projectPath)
        {
            string projectFullPath = ToFullPath(_solutionFolder, projectPath);
            if (!IsInFolder(projectFullPath, _solutionFolder))
            {
                return;
            }

            if (!File.Exists(projectFullPath))
            {
                _problems.Add($"project {projectFullPath} does not exist");
                return;
            }

            List<string> referencePaths;
            try
            {
                referencePaths = ReadProjectReferences(projectFullPath);
            }
            catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
            {
                _problems.Add($"cannot read {projectFullPath}: {e.Message}");
                return;
            }

            foreach (string referencePath in referencePaths)
            {
                AddReference(referencePath);
            }
        }

        //ერთი პრობლემაც Needs attention-ია, მის გარეშე ერთი დასაბრუნებელი პაკეტიც - Needs Distribute Packages
        public ProjectSolutionCheckResult CreateResult()
        {
            if (_problems.Count > 0)
            {
                return new ProjectSolutionCheckResult(EProjectSolutionCheckStatus.NeedsAttention,
                    [.. _problems.Distinct(StringComparer.OrdinalIgnoreCase)]);
            }

            return _packagesToDistribute.Count > 0
                ? new ProjectSolutionCheckResult(EProjectSolutionCheckStatus.NeedsDistributePackages,
                    [.. _packagesToDistribute.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal)])
                : new ProjectSolutionCheckResult(EProjectSolutionCheckStatus.Ok, []);
        }

        private void AddReference(string referenceFullPath)
        {
            if (IsInFolder(referenceFullPath, _solutionFolder))
            {
                return;
            }

            if (_parentFolder is null || !IsInFolder(referenceFullPath, _parentFolder))
            {
                _problems.Add($"far reference {referenceFullPath}");
                return;
            }

            //UseLocalPackages-გამორთულ პროექტს Distribute all packages არ ეხება: მისი წყაროს რეფერენსები განზრახაა
            if (!_project.UseLocalPackages)
            {
                return;
            }

            string? gitName = FindCloneGitName(referenceFullPath);
            if (gitName is null)
            {
                string repositoryFolderName =
                    Path.GetRelativePath(_parentFolder, referenceFullPath).Split(Path.DirectorySeparatorChar)[0];
                _problems.Add($"{repositoryFolderName} is not in the gits list");
                return;
            }

            ProjectModel? gitProject = _parameters.Projects.GetValueOrDefault(gitName);
            //პაკეტი არ არის (ფრონტი ან მხოლოდ ამ პროექტის კოდი), ანუ პროექტის ნაწილია
            if (gitProject is null || gitProject.ProjectType != EProjectType.IsPackage)
            {
                return;
            }

            if (gitProject.UseLocalPackages)
            {
                _packagesToDistribute.Add(gitName);
                return;
            }

            _problems.Add($"package {gitName} has UseLocalPackages off");
        }

        //პროექტის გიტი, რომლის კლონშიც რეფერენსი მიდის; ჩადგმულებიდან ყველაზე ღრმა იგებს
        private string? FindCloneGitName(string referenceFullPath)
        {
            return _project.GitProjectNames.Select(gitName => (GitName: gitName, CloneFolder: GetCloneFolder(gitName)))
                .Where(x => x.CloneFolder is not null && IsInFolder(referenceFullPath, x.CloneFolder))
                .OrderByDescending(x => x.CloneFolder?.Length ?? 0).Select(x => x.GitName).FirstOrDefault();
        }

        //PackageDistributor-ის წესით გიტის კლონი ProjectFolderName\GitProjectFolderName-შია.
        //{SpaProjectFolderRelativePath}-ით დაწყებული გიტი (ReactAppCarcass) ფრონტის კლონის შიგნითაა და csproj მას არ მიმართავს
        private string? GetCloneFolder(string gitName)
        {
            string? gitFolderName = _parameters.Gits.GetValueOrDefault(gitName)?.GitProjectFolderName;
            if (string.IsNullOrWhiteSpace(_project.ProjectFolderName) || string.IsNullOrWhiteSpace(gitFolderName) ||
                gitFolderName.StartsWith(GitDataModel.SpaProjectFolderRelativePathName, StringComparison.Ordinal))
            {
                return null;
            }

            return ToFullPath(_project.ProjectFolderName, gitFolderName);
        }

        //csproj-ები და .sln გზებს \-ით წერენ, ამიტომ Linux-ზე ის / -ით იცვლება
        private static string ToFullPath(string folder, string relativePath)
        {
            return Path.GetFullPath(Path.Combine(folder, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
        }

        //ფოლდერი მოწმდება გამყოფის საზღვარზე (D:\W\AppX არ არის D:\W\App-ის შიგნით), რეგისტრის გარეშე,
        //როგორც PackageDistributor-ში
        private static bool IsInFolder(string fullPath, string folder)
        {
            string folderPrefix = Path.EndsInDirectorySeparator(folder) ? folder : folder + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ReadProjectReferences(string projectFullPath)
        {
            //არსებული ფაილის სრულ გზას ფოლდერი ყოველთვის აქვს
            string projectFolder = Path.GetDirectoryName(projectFullPath)!;
            return
            [
                .. XDocument.Load(projectFullPath).Descendants().Where(x => x.Name.LocalName == "ProjectReference")
                    .Select(x => (string?)x.Attribute("Include")).OfType<string>()
                    .Select(include => ToFullPath(projectFolder, include))
            ];
        }
    }
}
