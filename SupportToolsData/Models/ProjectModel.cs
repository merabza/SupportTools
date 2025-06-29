using System;
using System.Collections.Generic;
using System.IO;
using LibDatabaseParameters;
using LibGitData;
using LibGitData.Models;
using LibParameters;

namespace SupportToolsData.Models;

public sealed class ProjectModel : ItemData
{
    public bool IsService { get; init; }
    public string? ProjectGroupName { get; init; }
    public string? ProjectDescription { get; set; }
    public bool UseAlternativeWebAgent { get; init; }
    public string? ProjectFolderName { get; init; }
    public string? SolutionFileName { get; init; }
    public string? ProjectSecurityFolderPath { get; init; }
    public string? MainProjectName { get; init; }
    public string? SpaProjectName { get; set; }
    public string? ProgramArchiveDateMask { get; set; }
    public string? ProgramArchiveExtension { get; set; }
    public string? ParametersFileDateMask { get; set; }
    public string? ParametersFileExtension { get; set; }
    public string? MigrationStartupProjectFilePath { get; set; }
    public string? MigrationProjectFilePath { get; set; }
    public string? SolutionFileNameWithMigrationProject { get; set; }
    public string? SeedProjectFilePath { get; set; }
    public string? SeedProjectParametersFilePath { get; set; }
    public string? DbContextName { get; init; }
    public string? ProjectShortPrefix { get; init; }
    public string? ScaffoldSeederProjectName { get; set; }
    public string? DbContextProjectName { get; init; }
    public string? NewDataSeedingClassLibProjectName { get; init; }
    public string? ExcludesRulesParametersFilePath { get; init; }
    public string? AppSetEnKeysJsonFileName { get; init; }
    public string? KeyGuidPart { get; init; }
    public string? MigrationSqlFilesFolder { get; set; }
    public DatabaseParameters? DevDatabaseParameters { get; init; }
    public DatabaseParameters? ProdCopyDatabaseParameters { get; init; }
    public List<string> RedundantFileNames { get; init; } = [];
    public List<string> FrontNpmPackageNames { get; init; } = [];
    public Dictionary<string, EndpointModel> Endpoints { get; init; } = [];
    public List<string> GitProjectNames { get; init; } = [];
    public List<string> ScaffoldSeederGitProjectNames { get; set; } = [];
    public Dictionary<string, ServerInfoModel> ServerInfos { get; init; } = [];
    public List<ETools> AllowToolsList { get; init; } = [];
    public string? PrepareProdCopyDatabaseProjectFilePath { get; set; }
    public string? PrepareProdCopyDatabaseProjectParametersFilePath { get; set; }

    public ServerInfoModel GetServerInfoRequired(string serverName)
    {
        return GetServerInfo(serverName) ??
               throw new InvalidOperationException($"Server with name {serverName} is not exists in project");
    }

    private ServerInfoModel? GetServerInfo(string serverName)
    {
        return ServerInfos.GetValueOrDefault(serverName);
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

    private string? ProjectFileName(string projectName, GitProjects gitProjects)
    {
        if (string.IsNullOrWhiteSpace(ProjectFolderName))
            return null;
        var gitProject = gitProjects.GetGitProjectByKey(projectName);
        var projectRelativePath = gitProject.ProjectRelativePath;
        var projectFileName = gitProject.ProjectFileName;
        return string.IsNullOrWhiteSpace(projectRelativePath) || string.IsNullOrWhiteSpace(projectFileName)
            ? null
            : Path.Combine(ProjectFolderName, projectRelativePath, projectFileName);
    }

    private static string? ProjectFolderRelativePath(string projectName, GitProjects gitProjects)
    {
        var gitProject = gitProjects.GetGitProjectByKey(projectName);
        var projectRelativePath = gitProject.ProjectRelativePath;
        return string.IsNullOrWhiteSpace(projectRelativePath) ? null : projectRelativePath;
    }

    public List<string> GetGitProjectNames(EGitCol gitCol)
    {
        return gitCol switch
        {
            EGitCol.Main => GitProjectNames,
            EGitCol.ScaffoldSeed => ScaffoldSeederGitProjectNames,
            _ => []
        };
    }
}