using System;
using System.Collections.Generic;
using System.IO;
using CliParametersDataEdit.Models;
using LibParameters;

namespace SupportToolsData.Models;

public sealed class ProjectModel : ItemData
{
    public string? ServiceName { get; set; }
    public bool UseAlternativeWebAgent { get; set; }
    public string? ProjectFolderName { get; set; }
    public string? SolutionFileName { get; set; }
    public string? ProjectSecurityFolderPath { get; set; }
    public string? MainProjectName { get; set; }
    public string? SpaProjectName { get; set; }
    public string? ProgramArchiveDateMask { get; set; }
    public string? ProgramArchiveExtension { get; set; }
    public string? ParametersFileDateMask { get; set; }
    public string? ParametersFileExtension { get; set; }
    public string? MigrationStartupProjectFilePath { get; set; }
    public string? MigrationProjectFilePath { get; set; }
    public string? SeedProjectFilePath { get; set; }
    public string? SeedProjectParametersFilePath { get; set; }
    public string? GetJsonFromScaffoldDbProjectFileFullName { get; set; }
    public string? GetJsonFromScaffoldDbProjectParametersFileFullName { get; set; }
    public string? DbContextName { get; set; }
    public string? ProjectShortPrefix { get; set; }
    public string? ScaffoldSeederProjectName { get; set; }
    public string? DbContextProjectName { get; set; }
    public string? NewDataSeedingClassLibProjectName { get; set; }
    public string? ExcludesRulesParametersFilePath { get; set; }
    public string? AppSetEnKeysJsonFileName { get; set; }
    public string? KeyGuidPart { get; set; }
    public string? MigrationSqlFilesFolder { get; set; }
    public DatabaseConnectionParameters? DevDatabaseConnectionParameters { get; set; }
    public DatabaseConnectionParameters? ProdCopyDatabaseConnectionParameters { get; set; }
    public List<string> RedundantFileNames { get; set; } = new();
    public List<string> GitProjectNames { get; set; } = new();
    public List<string> ScaffoldSeederGitProjectNames { get; set; } = new();
    public Dictionary<string, ServerInfoModel> ServerInfos { get; set; } = new();
    public List<ETools> AllowToolsList { get; set; } = new();

    public bool IsService => !string.IsNullOrWhiteSpace(ServiceName);

    public ServerInfoModel GetServerInfoRequired(string serverName)
    {
        return GetServerInfo(serverName) ??
               throw new InvalidOperationException($"Server with name {serverName} is not exists in project");
    }

    public ServerInfoModel? GetServerInfo(string serverName)
    {
        return !ServerInfos.ContainsKey(serverName) ? null : ServerInfos[serverName];
    }

    public string? MainProjectFolderRelativePath(GitProjects gitProjects)
    {
        return string.IsNullOrWhiteSpace(MainProjectName)
            ? null
            : ProjectFolderRelativePath(MainProjectName, gitProjects);
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

    public string? SpaProjectFileName(GitProjects gitProjects)
    {
        return string.IsNullOrWhiteSpace(SpaProjectName) ? null : ProjectFileName(SpaProjectName, gitProjects);
    }

    private string? ProjectFileName(string projectName, GitProjects gitProjects)
    {
        if (string.IsNullOrWhiteSpace(ProjectFolderName))
            return null;
        var gitProject = gitProjects.GetGitProjectByKey(projectName);
        var projectRelativePath = gitProject.ProjectRelativePath;
        return string.IsNullOrWhiteSpace(projectRelativePath)
            ? null
            : Path.Combine(ProjectFolderName, projectRelativePath);
    }

    private static string? ProjectFolderRelativePath(string projectName, GitProjects gitProjects)
    {
        var gitProject = gitProjects.GetGitProjectByKey(projectName);
        var projectRelativePath = gitProject.ProjectRelativePath;
        if (string.IsNullOrWhiteSpace(projectRelativePath))
            return null;
        return !projectRelativePath.Contains(Path.DirectorySeparatorChar)
            ? null
            : projectRelativePath[..projectRelativePath.LastIndexOf(Path.DirectorySeparatorChar)];
    }
}