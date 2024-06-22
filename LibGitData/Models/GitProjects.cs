using System;
using System.Collections.Generic;
using LibGitData.Domain;
using Microsoft.Extensions.Logging;

namespace LibGitData.Models;

public sealed class GitProjects
{
    private readonly Dictionary<string, GitProjectDataDomain> _gitProjects;

    private GitProjects(Dictionary<string, GitProjectDataDomain> gitProjects)
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
    public GitProjectDataDomain TestToolsApi => GetGitProjectByKey(nameof(TestToolsApi));
    public GitProjectDataDomain WebInstallers => GetGitProjectByKey(nameof(WebInstallers));
    public GitProjectDataDomain ConfigurationEncrypt => GetGitProjectByKey(nameof(ConfigurationEncrypt));
    public GitProjectDataDomain SerilogLogger => GetGitProjectByKey(nameof(SerilogLogger));
    public GitProjectDataDomain SwaggerTools => GetGitProjectByKey(nameof(SwaggerTools));
    public GitProjectDataDomain WindowsServiceTools => GetGitProjectByKey(nameof(WindowsServiceTools));
    public GitProjectDataDomain ReactTools => GetGitProjectByKey(nameof(ReactTools));
    public GitProjectDataDomain BackgroundTasksTools => GetGitProjectByKey(nameof(BackgroundTasksTools));
    public GitProjectDataDomain ServerCarcassMini => GetGitProjectByKey(nameof(ServerCarcassMini));

    public static GitProjects Create(ILogger? logger, Dictionary<string, GitProjectDataModel> gitPrs)
    {
        Dictionary<string, GitProjectDataDomain> gitProjects = [];
        foreach (var (key, value) in gitPrs)
        {
            if (string.IsNullOrWhiteSpace(value.GitName))
            {
                logger?.LogError("GitName is empty for Git Project with key {key})", key);
                continue;
            }

            if (string.IsNullOrWhiteSpace(value.ProjectRelativePath))
            {
                logger?.LogError("ProjectRelativePath is empty for Git Project with key {key})", key);
                continue;
            }

            var dependsOnProjectNames = value.DependsOnProjectNames;

            gitProjects.Add(key,
                new GitProjectDataDomain(value.GitName, value.ProjectRelativePath,
                    dependsOnProjectNames));
        }

        return new GitProjects(gitProjects);
    }

    public GitProjectDataDomain GetGitProjectByKey(string key)
    {
        if (_gitProjects.TryGetValue(key, out var byKey))
            return byKey;
        throw new Exception($"GitProject With Key {key} does not found");
    }

    public GitProjectDataDomain? GetGitProjectIfExistsByKey(string key)
    {
        return _gitProjects.GetValueOrDefault(key);
    }
}