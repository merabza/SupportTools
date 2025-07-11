using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeTools;
using JetBrainsResharperGlobalToolsWork;
using LibAppProjectCreator.Models;
using LibDataInput;
using LibDotnetWork;
using LibGitData.Domain;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.ToolActions;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.AppCreators;

public abstract class AppCreatorBase
{
    private readonly GitRepos _gitRepos;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly int _indentSize;
    private List<string> FoldersForCreate { get; } = [];//შესაქმნელი ფოლდერების სია
    private List<string> FoldersForCheckAndClear { get; } = [];//გასაწმენდი ფოლდერების სია
    private List<ProjectBase> Projects { get; } = [];//სოლუშენში ჩასამატებელი პროექტების სია
    private List<ReferenceDataModel> References { get; } = [];//რეფერენსების სია
    private List<PackageDataModel> Packages { get; } = [];//გარე პაკეტების სია

    protected readonly GitProjects GitProjects;
    protected readonly ILogger Logger;
    protected readonly string ProjectName;
    protected string WorkPath { get; }
    protected string SecurityPath { get; }

    public string SolutionPath { get; }
    public List<GitCloneDataModel> GitClones { get; } = [];//გიტით დასაკლონი პროექტების სია

    protected AppCreatorBase(ILogger logger, IHttpClientFactory httpClientFactory, string projectName,
        int indentSize, GitProjects gitProjects, GitRepos gitRepos, string workPath, string securityPath,
        string solutionPath)
    {
        Logger = logger;
        _httpClientFactory = httpClientFactory;
        ProjectName = projectName;
        _indentSize = indentSize;
        GitProjects = gitProjects;
        _gitRepos = gitRepos;
        WorkPath = workPath;
        SecurityPath = securityPath;
        SolutionPath = solutionPath;
    }

    public async Task<bool> PrepareParametersAndCreateApp(bool askForDelete, CancellationToken cancellationToken,
        ECreateAppVersions createAppVersions = ECreateAppVersions.DoAll)
    {
        if (!PrepareParameters())
        {
            StShared.WriteErrorLine("Solution Parameters does not created", true, Logger);
            return false;
        }

        if (await CreateApp(askForDelete, createAppVersions, cancellationToken))
        {
            return true;
        }

        StShared.WriteErrorLine("Solution does not created", true, Logger);
        return false;
    }

    //სოლუშენში შემავალი პროექტების მომზადება
    protected virtual void PrepareProjectsData()
    {
    }
    
    protected void AddProject(ProjectForCreate projectForCreate)
    {
        FoldersForCreate.Add(projectForCreate.ProjectFullPath);

        foreach (var folderFullPath in projectForCreate.FoldersForCreate.Values)
            //ჩაემატოს შესაქმნელი ფოლდერების სიაში
        {
            FoldersForCreate.Add(folderFullPath);
        }

        Projects.Add(projectForCreate);
    }

    //პროექტის ტიპზე დამოკიდებული დამატებით საჭირო ფაილების შექმნა
    protected abstract Task<bool> MakeAdditionalFiles(CancellationToken cancellationToken = default);

    //პროექტის ტიპისათვის დამახასიათებელი დამატებითი პარამეტრების გამოანგარიშება
    protected virtual bool PrepareSpecific()
    {
        return true;
    }

    protected void AddReference(ProjectBase firstProjectData, GitProjectDataDomain referenceProjectData)
    {
        var gitProject = Projects.SingleOrDefault(x => x.ProjectName == referenceProjectData.ProjectName) ??
                         AddGitProject(WorkPath, referenceProjectData);
        if (gitProject is not null)
        {
            AddReference(firstProjectData, gitProject);
        }
    }

    protected void AddReference(ProjectBase firstProjectData, ProjectBase referenceProjectData)
    {
        References.Add(new ReferenceDataModel(firstProjectData.ProjectFileFullName,
            referenceProjectData.ProjectFileFullName));
    }

    protected void AddPackage(ProjectForCreate projectData, string packageName, string? version = null)
    {
        Packages.Add(new PackageDataModel(projectData.ProjectFileFullName, packageName, version));
    }

    private void PrepareFoldersForCheckAndClear()
    {
        //FoldersForCheckAndClear.Add(SolutionPath);
        FoldersForCheckAndClear.Add(SecurityPath);

        //foreach (var projectBase in Projects.Where(projectBase =>
        //             !string.IsNullOrWhiteSpace(projectBase.SolutionFolderName)))
        foreach (var createInPath in Projects.Select(x => x.CreateInPath).Distinct())
        {
            FoldersForCheckAndClear.Add(createInPath);
        }
    }

    private void PrepareFoldersForCreate()
    {
        //FoldersForCreate.Add(WorkPath);
        FoldersForCreate.Add(SolutionPath);
        FoldersForCreate.Add(SecurityPath);
        foreach (var folder in FoldersForCheckAndClear)
        {
            FoldersForCreate.Add(folder);
        }
    }

    private bool PrepareParameters()
    {
        Stats.IndentSize = _indentSize < 1 ? 4 : _indentSize;
        PrepareProjectsData();
        PrepareFoldersForCheckAndClear();
        PrepareFoldersForCreate();

        //პროექტის ტიპისათვის დამახასიათებელი დამატებითი პარამეტრების გამოანგარიშება
        return PrepareSpecific();
    }

    private void AddGitClone(GitDataDto gitData)
    {
        if (GitClones.Any(x => x.GitProjectName == gitData.GitProjectAddress))
        {
            return;
        }

        GitClones.Add(new GitCloneDataModel(gitData.GitProjectAddress, gitData.GitProjectFolderName));
    }

    private ProjectBase? AddGitProject(string createInPath, GitProjectDataDomain? projectData)
    {
        if (projectData == null)
        {
            return null;
        }

        //გავიაროთ დამოკიდებული პროექტები, თუკი არსებობს ისინი
        foreach (var gitProjectName in projectData.DependsOnProjectNames)
        {
            AddGitProject(createInPath, GitProjects.GetGitProjectIfExistsByKey(gitProjectName));
        }

        //თუ სიაში უკვე გვაქვს ეს პროექტი, აღარ ვამატებთ
        var foundedProjectDataModel = Projects.SingleOrDefault(x => x.ProjectName == projectData.ProjectName);

        if (foundedProjectDataModel is not null)
        {
            return foundedProjectDataModel;
        }

        //დავიანგარიშოთ პროექტის ფაილის გზა
        var projectFileFullName =
            Path.Combine(createInPath, projectData.ProjectRelativePath, projectData.ProjectFileName);
        var projectFile = new FileInfo(projectFileFullName);
        var projectPath = projectFile.DirectoryName;
        if (projectPath is null)
        {
            return null;
        }

        var gitRepo = _gitRepos.GetGitRepoByKey(projectData.GitName);
        if (gitRepo is null)
        {
            return null;
        }

        //შევქმნათ პროექტის მოდელი
        ProjectFromGit projectFromGit = new(gitRepo.GitProjectFolderName, projectData.ProjectName, createInPath,
            projectData.ProjectRelativePath, projectFile.Name);
        //დავამატოთ პროექტების სიაში
        Projects.Add(projectFromGit);
        //დავამატოთ დასაკლონი პროექტების სიაში შესაბამისი გიტის პროექტი
        AddGitClone(gitRepo);
        //დავაბრუნოთ ახლადშექმნილი პროექტი
        return projectFromGit;
    }

    //აპლიკაციის შექმნის პროცესი
    private async Task<bool> CreateApp(bool askForDelete, ECreateAppVersions createAppVersions,
        CancellationToken cancellationToken = default)
    {
        if (!AppGitsSync(createAppVersions == ECreateAppVersions.Temp))
        {
            return false;
        }

        if (createAppVersions == ECreateAppVersions.OnlySyncGit)
        {
            return true;
        }

        //შევამოწმოთ და თუ შესაძლებელია წავშალოთ გასასუფთავებელი ფოლდერები
        if (FoldersForCheckAndClear.Any(folder => !Stat.CheckRequiredFolder(true, folder, askForDelete)))
        {
            return false;
        }

        //შევქმნათ ფოლდერების სიაში არსებული ყველა ფოლდერი
        if (FoldersForCreate.Any(folder => !StShared.CreateFolder(folder, true)))
        {
            return false;
        }

        //სოლუშენის შექმნა
        var dotnetProcessor = new DotnetProcessor(Logger, true);

        if (dotnetProcessor.CreateNewSolution(SolutionPath, ProjectName).IsSome)
        {
            return false;
        }

        //პროექტების დამატება სოლუშენში
        foreach (var prj in Projects)
        {
            switch (prj)
            {
                case ProjectForCreate projectForCreate:
                {
                    if (projectForCreate.DotnetProjectType == EDotnetProjectType.ReactEsProj)
                    {
                        //რეაქტის პროექტის შექმნა ფრონტისთვის
                        var reactEsProjectCreator = new ReactEsProjectCreator(Logger, _httpClientFactory,
                            projectForCreate.CreateInPath, projectForCreate.ProjectFolderName,
                            projectForCreate.ProjectFileName, projectForCreate.ProjectName, true);
                        if (!reactEsProjectCreator.Create())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        //პროექტების შექმნა
                        if (dotnetProcessor.CreateNewProject(projectForCreate.DotnetProjectType,
                                projectForCreate.ProjectCreateParameters, projectForCreate.ProjectFullPath,
                                projectForCreate.ProjectName).IsSome)
                        {
                            return false;
                        }

                        var projectXml = XElement.Load(projectForCreate.ProjectFileFullName);

                        AddProjectParametersWithCheck(projectXml, "PropertyGroup", "ImplicitUsings", "disable");

                        projectXml.Save(projectForCreate.ProjectFileFullName);

                        if (projectForCreate.ClassForDelete != null)
                        {
                            //წაიშალოს არსებული ავტომატურად შექმნილი Program.cs
                            var programCs = Path.Combine(projectForCreate.ProjectFullPath,
                                $"{projectForCreate.ClassForDelete}.cs");
                            File.Delete(programCs);
                        }
                    }

                    if (dotnetProcessor.AddProjectToSolution(SolutionPath, projectForCreate.SolutionFolderName,
                            projectForCreate.ProjectFileFullName).IsSome)
                    {
                        return false;
                    }

                    break;
                }
                case ProjectFromGit projectFromGit:
                {
                    var projPath = Path.Combine(WorkPath, projectFromGit.GitProjectFolderName,
                        projectFromGit.ProjectName, $"{projectFromGit.ProjectName}.csproj");
                    if (dotnetProcessor
                        .AddProjectToSolution(SolutionPath, projectFromGit.SolutionFolderName, projPath).IsSome)
                    {
                        return false;
                    }

                    break;
                }
            }
        }

        if (!await MakeAdditionalFiles(cancellationToken))
            return false;

        //რეფერენსების მიერთება პროექტებში, სიის მიხედვით
        foreach (var refData in References)
        {
            if (!File.Exists(refData.ProjectFilePath))
            {
                StShared.WriteErrorLine($"{refData.ProjectFilePath} is not exists", true, Logger, false);
                continue;
            }

            if (!File.Exists(refData.ReferenceProjectFilePath))
            {
                StShared.WriteErrorLine($"{refData.ReferenceProjectFilePath} is not exists", true, Logger, false);
                continue;
            }

            if (dotnetProcessor.AddReferenceToProject(refData.ProjectFilePath, refData.ReferenceProjectFilePath)
                .IsSome)
            {
                return false;
            }
        }

        //პაკეტების მიერთება პროექტებში, სიის მიხედვით
        if (!Packages.All(packageData =>
                dotnetProcessor.AddPackageToProject(packageData.ProjectFilePath, packageData.PackageName,
                    packageData.Version).IsNone))
        {
            return false;
        }

        var jb = new JetBrainsResharperGlobalToolsProcessor(Logger, true);

        if (jb.Cleanupcode(SolutionPath).IsSome)
        {
            return false;
        }

        if (createAppVersions == ECreateAppVersions.Temp)
        {
            return true;
        }

        var gitProcessor = new GitProcessor(true, Logger, SolutionPath);
        if (gitProcessor.Initialise().IsSome)
        {
            return false;
        }

        return gitProcessor.Add() && gitProcessor.Commit("Initial");
    }

    private static void AddProjectParametersWithCheck(XElement projectXml, string groupName, string propertyName,
        string propertyValue)
    {
        var grpXml = CheckAddProjectGroup(projectXml, groupName);
        AddProjectParametersWithCheck(grpXml, propertyName, propertyValue);
    }

    private static XElement CheckAddProjectGroup(XElement projectXml, string groupName)
    {
        var grpXml = projectXml.Descendants(groupName).SingleOrDefault();
        if (grpXml is not null)
        {
            return grpXml;
        }

        grpXml = new XElement(groupName);
        projectXml.Add(grpXml);

        return grpXml;
    }

    private static void AddProjectParametersWithCheck(XElement grpXml, string propertyName, string propertyValue)
    {
        var propXml = grpXml.Descendants(propertyName).SingleOrDefault();
        if (propXml is null)
        {
            propXml = new XElement(propertyName, propertyValue);
            grpXml.Add(propXml);
        }
        else
        {
            propXml.Value = propertyValue;
        }
    }

    private bool AppGitsSync(bool useProjectUpdater)
    {
        var gitProjectNames = GitClones.Select(x => x.GitProjectFolderName).ToList();

        var gitSyncAll = new SyncOneProjectAllGitsToolAction(Logger,
            new SyncOneProjectAllGitsParameters(null, WorkPath,
                _gitRepos.Gits.Where(x => gitProjectNames.Contains(x.Key)).Select(x => x.Value).ToList(), null,
                true, useProjectUpdater));
        return gitSyncAll.Run(CancellationToken.None).Result;
    }
}