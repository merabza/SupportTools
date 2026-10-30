using System;
using System.Collections.Generic;
using System.IO;
using LibGitData.Domain;
using LibGitData.Models;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;

namespace SupportToolsData.Models;

public sealed class ProjectModel : ItemData
{
    public EProjectType ProjectType { get; init; }
    public string? ProjectGroupName { get; init; }
    public string? ProjectDescription { get; set; }
    public int MajorVersion { get; set; } = 1;
    public int MinorVersion { get; set; }
    public bool UseAlternativeWebAgent { get; init; }

    //პროექტი იყენებს ლოკალურ (საკუთარ) პაკეტებს. თუ გამორთულია, Distribute all packages და
    //Reverse Package Distribution ამ პროექტს არ ეხება
    public bool UseLocalPackages { get; set; }

    //პროექტი მონაცემთა ბაზას იყენებს. გამორთულისას რედაქტორებში ბაზასთან დაკავშირებული პარამეტრები და ინსტრუმენტები
    //იმალება. ნაგულისხმევად ჩართულია, რომ ძველ პარამეტრების ფაილში (ველის გარეშე) პროექტები ისევ ისე ჩანდეს
    public bool UsesDatabase { get; set; } = true;

    public string? ProjectFolderName { get; init; }
    public string? SolutionFileName { get; init; }

    //.editorconfig შაბლონის სახელი EditorConfigPatterns სიიდან. აუცილებელი არ არის: თუ ცარიელია, პროექტის .editorconfig ფაილი არ მოწმდება
    public string? EditorConfigPatternName { get; set; }
    public string? ProjectSecurityFolderPath { get; init; }
    public string? MainProjectName { get; init; }
    public string? ApiContractsProjectName { get; init; }
    public string? SpaProjectName { get; set; }
    public string? ProgramArchiveDateMask { get; set; }
    public string? ProgramArchiveExtension { get; set; }
    public string? ParametersFileDateMask { get; set; }
    public string? ParametersFileExtension { get; set; }
    public string? MigrationStartupProjectFilePath { get; set; }
    public string? DataSeederRulesByTableStartupProjectFilePath { get; set; }
    public string? OldDataConvertorForDataSeeder { get; set; }

    public string? MigrationProjectFilePath { get; set; }

    //public string? SolutionFileNameWithMigrationProject { get; set; }
    public string? DbContextName { get; init; }
    public string? AppSetEnKeysJsonFileName { get; init; }
    public string? KeyGuidPart { get; init; }
    public DatabaseParameters? DevDatabaseParameters { get; init; }
    public DatabaseParameters? ProdCopyDatabaseParameters { get; init; }
    public List<string> RedundantFileNames { get; init; } = [];
    public List<string> FrontNpmPackageNames { get; init; } = [];
    public Dictionary<string, EndpointModel> Endpoints { get; init; } = [];
    public Dictionary<string, RouteClassModel> RouteClasses { get; init; } = [];
    public List<string> GitProjectNames { get; init; } = [];
    public Dictionary<string, ServerInfoModel> ServerInfos { get; init; } = [];
    public List<EProjectTools> AllowToolsList { get; init; } = [];
    public string? PairedDbObjectsResultFileName { get; set; }

    public ServerInfoModel GetServerInfoRequired(string serverName)
    {
        return GetServerInfo(serverName) ??
               throw new InvalidOperationException($"Server with name {serverName} is not exists in project");
    }

    private ServerInfoModel? GetServerInfo(string serverName)
    {
        return ServerInfos.GetValueOrDefault(serverName);
    }

    //პროექტის .editorconfig ფაილი solution ფაილის ფოლდერში მდებარეობს. თუ solution ფაილი მითითებული არ არის, აბრუნებს null
    public string? EditorConfigFileName()
    {
        string? solutionFolder = string.IsNullOrWhiteSpace(SolutionFileName)
            ? null
            : Path.GetDirectoryName(SolutionFileName);
        return string.IsNullOrWhiteSpace(solutionFolder) ? null : Path.Combine(solutionFolder, ".editorconfig");
    }

    public string? MainProjectFileName(GitProjects gitProjects)
    {
        return string.IsNullOrWhiteSpace(MainProjectName) ? null : ProjectFileName(MainProjectName, gitProjects);
    }

    public string? SpaProjectFolderRelativePath(GitProjects gitProjects)
    {
        return string.IsNullOrWhiteSpace(SpaProjectName)
            ? null
            : ProjectFolderRelativePath(SpaProjectName, gitProjects);
    }

    //SpaProjectName შევსებულია, მაგრამ GitProjects-ში მისი ჩანაწერი არ არის (Update Git Projects ჯერ არ გაშვებულა),
    //ამიტომ SpaProjectFolderRelativePath exception-ს ისვრის
    public bool IsSpaProjectMissing(GitProjects gitProjects)
    {
        return !string.IsNullOrWhiteSpace(SpaProjectName) &&
               gitProjects.GetGitProjectIfExistsByKey(SpaProjectName) is null;
    }

    private string? ProjectFileName(string projectName, GitProjects gitProjects)
    {
        if (string.IsNullOrWhiteSpace(ProjectFolderName))
        {
            return null;
        }

        GitProjectDataDomain gitProject = gitProjects.GetGitProjectByKey(projectName);
        string projectRelativePath = gitProject.ProjectRelativePath;
        string projectFileName = gitProject.ProjectFileName;
        return string.IsNullOrWhiteSpace(projectRelativePath) || string.IsNullOrWhiteSpace(projectFileName)
            ? null
            : Path.Combine(ProjectFolderName, projectRelativePath, projectFileName);
    }

    private static string? ProjectFolderRelativePath(string projectName, GitProjects gitProjects)
    {
        GitProjectDataDomain gitProject = gitProjects.GetGitProjectByKey(projectName);
        string projectRelativePath = gitProject.ProjectRelativePath;
        return string.IsNullOrWhiteSpace(projectRelativePath) ? null : projectRelativePath;
    }
}
