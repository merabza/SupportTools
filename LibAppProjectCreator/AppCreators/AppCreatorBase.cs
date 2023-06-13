﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CodeTools;
using LibAppProjectCreator.Git;
using LibAppProjectCreator.Models;
using LibAppProjectCreator.ProjectFileXmlModifiers;
using LibDataInput;
using Microsoft.Extensions.Logging;
using SupportToolsData.Domain;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.AppCreators;

public /*open*/ class AppCreatorBase
{
    private readonly GitRepos _gitRepos;
    protected readonly GitProjects GitProjects;
    protected readonly ILogger Logger;
    protected readonly AppProjectCreatorData Par;


    protected AppCreatorBase(ILogger logger, AppProjectCreatorData par, GitProjects gitProjects,
        GitRepos gitRepos, AppCreatorBaseData appCreatorBaseData)
    {
        Logger = logger;
        Par = par;
        GitProjects = gitProjects;
        _gitRepos = gitRepos;
        WorkPath = appCreatorBaseData.WorkPath;
        SecurityPath = appCreatorBaseData.SecurityPath;
        SolutionPath = appCreatorBaseData.SolutionPath;
        ForTest = appCreatorBaseData.ForTest;
    }

    protected string WorkPath { get; }

    protected string SecurityPath { get; }

    protected string SolutionPath { get; }
    protected bool ForTest { get; }

    //შესაქმნელი ფოლდერების სია
    protected List<string> FoldersForCreate { get; } = new();

    //გასაწმენდი ფოლდერების სია
    protected List<string> FoldersForCheckAndClear { get; } = new();

    //გიტით დასაკლონი პროექტების სია
    public List<GitCloneDataModel> GitClones { get; } = new();

    //სოლუშენში ჩასამატებელი პროექტების სია
    private List<ProjectBase> Projects { get; } = new();

    //რეფერენსების სია
    private List<ReferenceDataModel> References { get; } = new();

    //გარე პაკეტების სია
    private List<PackageDataModel> Packages { get; } = new();


    //სოლუშენში შემავალი პროექტების მომზადება
    protected virtual void PrepareProjectsData()
    {
    }

    //პროექტის ტიპზე დამოკიდებული დამატებით საჭირო ფაილების შექმნა
    protected virtual bool MakeAdditionalFiles()
    {
        return true;
    }

    //პროექტის ტიპისათვის დამახასიათებელი დამატებითი პარამეტრების გამოანგარიშება
    protected virtual bool PrepareSpecific()
    {
        return true;
    }

    protected virtual void PrepareFoldersForCheckAndClear()
    {
        FoldersForCheckAndClear.Add(SolutionPath);
        FoldersForCheckAndClear.Add(SecurityPath);
    }

    protected virtual void PrepareFoldersForCreate()
    {
        FoldersForCreate.Add(WorkPath);
        FoldersForCreate.Add(SolutionPath);
        FoldersForCreate.Add(SecurityPath);
    }

    public bool PrepareParameters()
    {
        Stats.IndentSize = Par.IndentSize < 1 ? 4 : Par.IndentSize;
        PrepareFoldersForCheckAndClear();
        PrepareFoldersForCreate();
        PrepareProjectsData();

        //პროექტის ტიპისათვის დამახასიათებელი დამატებითი პარამეტრების გამოანგარიშება
        return PrepareSpecific();
    }

    public bool PrepareParametersAndCreateApp()
    {
        return PrepareParameters() && CreateApp();
    }

    protected void AddGitClone(string createInPath, GitDataDomain gitData)
    {
        if (GitClones.Any(x => x.GitProjectName == gitData.GitProjectAddress))
            return;
        GitClones.Add(new GitCloneDataModel(createInPath, gitData.GitProjectAddress,
            gitData.GitProjectFolderName));
    }

    private ProjectBase? AddGitProject(string createInPath, GitProjectDataDomain? projectData)
    {
        if (projectData == null)
            return null;

        //გავიაროთ დამოკიდებული პროექტები, თუკი არსებობს ისინი
        foreach (var gitProjectName in projectData.DependsOnProjectNames)
            AddGitProject(createInPath, GitProjects.GetGitProjectIfExistsByKey(gitProjectName));

        //თუ სიაში უკვე გვაქვს ეს პროექტი, აღარ ვამატებთ
        var foundedProjectDataModel = Projects.SingleOrDefault(x => x.ProjectName == projectData.ProjectName);

        if (foundedProjectDataModel is not null)
            return foundedProjectDataModel;

        //დავიანგარიშოთ პროექტის ფაილის გზა
        var projectFileFullName = Path.Combine(createInPath, projectData.ProjectRelativePath);
        var projectFile = new FileInfo(projectFileFullName);
        var projectPath = projectFile.DirectoryName;
        if (projectPath is null)
            return null;
        var gitRepo = _gitRepos.GetGitRepoByKey(projectData.GitName);
        if (gitRepo is null)
            return null;
        //შევქმნათ პროექტის მოდელი
        ProjectFromGit projectFromGit = new(gitRepo.GitProjectFolderName, projectData.ProjectName, projectPath,
            projectFileFullName);
        //დავამატოთ პროექტების სიაში
        Projects.Add(projectFromGit);
        //დავამატოთ დასაკლონი პროექტების სიაში შესაბამისი გიტის პროექტი
        AddGitClone(createInPath, gitRepo);
        //დავაბრუნოთ ახლადშექმნილი პროექტი
        return projectFromGit;
    }

    protected void AddReference(ProjectBase firstProjectData, GitProjectDataDomain referenceProjectData)
    {
        var gitProject =
            Projects.SingleOrDefault(x => x.ProjectName == referenceProjectData.ProjectName) ??
            AddGitProject(WorkPath, referenceProjectData);
        if (gitProject is not null)
            AddReference(firstProjectData, gitProject);
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

    //აპლიკაციის შექმნის პროცესი
    public bool CreateApp()
    {
        //შევამოწმოთ და თუ შესაძლებელია წავშალოთ გასასუფთავებელი ფოლდერები
        if (FoldersForCheckAndClear.Any(folder => !Stat.CheckRequiredFolder(true, folder, !ForTest)))
            return false;

        //შევქმნათ ფოლდერების სიაში არსებული ყველა ფოლდერი
        if (FoldersForCreate.Any(folder => !StShared.CreateFolder(folder, true)))
            return false;


        if (!AppGitsSync())
            return false;

        //სოლუშენის შექმნა
        if (!StShared.RunProcess(true, Logger, "dotnet", $"new sln --output {SolutionPath} --name {Par.ProjectName}"))
            return false;

        //პროექტების დამატება სოლუშენში
        foreach (var prj in Projects)
        {
            string projPath;
            if (prj is ProjectForCreate projectForCreate)
            {
                //პროექტების შექმნა
                if (!StShared.RunProcess(true, Logger, "dotnet",
                        $"new {projectForCreate.DotnetProjectType.ToString().ToLower()}{(string.IsNullOrWhiteSpace(projectForCreate.ProjectCreateParameters) ? "" : $" {projectForCreate.ProjectCreateParameters}")} --output {projectForCreate.ProjectFullPath} --name {projectForCreate.ProjectName}"))
                    return false;

                var projectXml = XElement.Load(projectForCreate.ProjectFileFullName);

                AddProjectParametersWithCheck(projectXml, "PropertyGroup", "ImplicitUsings", "disable");

                if (projectForCreate.UseReact)
                {
                    var projectFileXmlModifierForReact = new ProjectFileXmlModifierForReact(projectXml);
                    if (!projectFileXmlModifierForReact.Run())
                        return false;
                }

                projectXml.Save(projectForCreate.ProjectFileFullName);

                if (projectForCreate.ClassForDelete != null)
                {
                    //წაიშალოს არსებული ავტომატურად შექმნილი Program.cs
                    var programCs = Path.Combine(projectForCreate.ProjectFullPath,
                        $"{projectForCreate.ClassForDelete}.cs");
                    File.Delete(programCs);
                }

                //projPath = Path.Combine(SolutionPath, projectForCreate.ProjectName,
                //    $"{projectForCreate.ProjectName}.csproj");
                if (!StShared.RunProcess(true, Logger, "dotnet",
                        $"sln {SolutionPath} add {(projectForCreate.SolutionFolderName is null ? "" : $"--solution-folder {projectForCreate.SolutionFolderName} ")}{projectForCreate.ProjectFileFullName}"))
                    return false;
            }
            else if (prj is ProjectFromGit projectFromGit)

            {
                projPath = Path.Combine(WorkPath, projectFromGit.GitProjectFolderName, projectFromGit.ProjectName,
                    $"{projectFromGit.ProjectName}.csproj");
                if (!StShared.RunProcess(true, Logger, "dotnet",
                        $"sln {SolutionPath} add {(projectFromGit.SolutionFolderName is null ? "" : $"--solution-folder {projectFromGit.SolutionFolderName} ")}{projPath}"))
                    return false;
            }
        }

        //რეფერენსების მიერთება პროექტებში, სიის მიხედვით
        if (References.Any(refData =>
                !StShared.RunProcess(true, Logger, "dotnet",
                    $"add {refData.ProjectFilePath} reference {refData.ReferenceProjectFilePath}")))
            return false;

        //პაკეტების მიერთება პროექტებში, სიის მიხედვით
        if (!Packages.All(packageData => StShared.RunProcess(true, Logger, "dotnet",
                $"add {packageData.ProjectFilePath} package {packageData.PackageName}{(packageData.Version == null ? "" : $" --version {packageData.Version}")}")))
            return false;

        if (!MakeAdditionalFiles())
            return false;

        if (!StShared.RunProcess(true, Logger, "jb", $"cleanupcode {SolutionPath}"))
            return false;

        if (!StShared.RunProcess(true, Logger, "git", $"-C \"{SolutionPath}\" init"))
            return false;

        if (!StShared.RunProcess(true, Logger, "git", $"-C \"{SolutionPath}\" add ."))
            return false;

        if (!StShared.RunProcess(true, Logger, "git", $"-C \"{SolutionPath}\" commit -m \"Initial\""))
            return false;

        return true;
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
        if (grpXml is null)
        {
            grpXml = new XElement(groupName);
            projectXml.Add(grpXml);
        }

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

    public bool AppGitsSync()
    {
        var gitProjectNames = GitClones.Select(x => x.GitProjectFolderName).ToList();

        var gitSyncAll = new GitSyncAll(Logger, WorkPath,
            _gitRepos.Gits.Where(x => gitProjectNames.Contains(x.Key)).Select(x => x.Value));
        gitSyncAll.Run();
        return true;
    }

    protected void AddProject(ProjectForCreate projectForCreate)
    {
        FoldersForCreate.Add(projectForCreate.ProjectFullPath);

        foreach (var folderFullPath in projectForCreate.FoldersForCreate.Values)
            //ჩაემატოს შესაქმნელი ფოლდერების სიაში
            FoldersForCreate.Add(folderFullPath);

        Projects.Add(projectForCreate);
    }
}