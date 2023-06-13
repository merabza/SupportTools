using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SupportToolsData.Domain;

namespace SupportToolsData.Models;

public sealed class GitProjects
{
    private readonly Dictionary<string, GitProjectDataDomain> _gitProjects;

    public GitProjects(Dictionary<string, GitProjectDataDomain> gitProjects)
    {
        _gitProjects = gitProjects;
    }

    public GitProjectDataDomain CarcassDb => GetGitProjectByKey(nameof(CarcassDb));
    public GitProjectDataDomain CarcassDataSeeding => GetGitProjectByKey(nameof(CarcassDataSeeding));
    public GitProjectDataDomain CarcassIdentity => GetGitProjectByKey(nameof(CarcassIdentity));

    public GitProjectDataDomain CarcassRepositories => GetGitProjectByKey(nameof(CarcassRepositories));

    //public GitProjectDataDomain CarcassJsonModels => GetGitProjectByKey(nameof(CarcassJsonModels));
    public GitProjectDataDomain CliParameters => GetGitProjectByKey(nameof(CliParameters));
    public GitProjectDataDomain CliTools => GetGitProjectByKey(nameof(CliTools));
    public GitProjectDataDomain CliParametersDataEdit => GetGitProjectByKey(nameof(CliParametersDataEdit));
    public GitProjectDataDomain CliToolsData => GetGitProjectByKey(nameof(CliToolsData));
    public GitProjectDataDomain DbTools => GetGitProjectByKey(nameof(DbTools));
    public GitProjectDataDomain DbContextAnalyzer => GetGitProjectByKey(nameof(DbContextAnalyzer));
    public GitProjectDataDomain SystemToolsShared => GetGitProjectByKey(nameof(SystemToolsShared));
    public GitProjectDataDomain ServerCarcass => GetGitProjectByKey(nameof(ServerCarcass));
    public GitProjectDataDomain TestToolsMini => GetGitProjectByKey(nameof(TestToolsMini));
    public GitProjectDataDomain WebInstallers => GetGitProjectByKey(nameof(WebInstallers));
    public GitProjectDataDomain ConfigurationEncrypt => GetGitProjectByKey(nameof(ConfigurationEncrypt));
    public GitProjectDataDomain SerilogLogger => GetGitProjectByKey(nameof(SerilogLogger));
    public GitProjectDataDomain SwaggerTools => GetGitProjectByKey(nameof(SwaggerTools));
    public GitProjectDataDomain WindowsServiceTools => GetGitProjectByKey(nameof(WindowsServiceTools));
    public GitProjectDataDomain ReactTools => GetGitProjectByKey(nameof(ReactTools));
    public GitProjectDataDomain BackgroundTasksTools => GetGitProjectByKey(nameof(BackgroundTasksTools));
    public GitProjectDataDomain ServerCarcassMini => GetGitProjectByKey(nameof(ServerCarcassMini));

    public static GitProjects Create(ILogger logger, Dictionary<string, GitProjectDataModel> gitPrs)
    {
        Dictionary<string, GitProjectDataDomain> gitProjects = new();
        foreach (var kvp in gitPrs)
        {
            if (string.IsNullOrWhiteSpace(kvp.Value.GitName))
            {
                logger.LogError("GitName is empty for Git Project with key {0})", kvp.Key);
                continue;
            }

            if (string.IsNullOrWhiteSpace(kvp.Value.ProjectRelativePath))
            {
                logger.LogError("ProjectRelativePath is empty for Git Project with key {0})", kvp.Key);
                continue;
            }

            var dependsOnProjectNames = kvp.Value.DependsOnProjectNames;

            gitProjects.Add(kvp.Key,
                new GitProjectDataDomain(kvp.Value.GitName, kvp.Value.ProjectRelativePath,
                    dependsOnProjectNames));
        }

        return new GitProjects(gitProjects);
    }

    public GitProjectDataDomain GetGitProjectByKey(string key)
    {
        if (_gitProjects.ContainsKey(key))
            return _gitProjects[key];
        throw new Exception($"GitProject With Key {key} does not found");
    }

    public GitProjectDataDomain? GetGitProjectIfExistsByKey(string key)
    {
        return _gitProjects.ContainsKey(key) ? _gitProjects[key] : null;
    }
}