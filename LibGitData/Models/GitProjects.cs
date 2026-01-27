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

    private const string BackendCarcass = nameof(BackendCarcass);

    private const string Db = nameof(Db);
    public GitProjectDataDomain BackendCarcassDb => GetGitProjectByKey($"{BackendCarcass}.{Db}");
    
    private const string DataSeeding = nameof(DataSeeding);
    public GitProjectDataDomain BackendCarcassDataSeeding => GetGitProjectByKey($"{BackendCarcass}.{DataSeeding}");
    
    private const string Identity = nameof(Identity);
    public GitProjectDataDomain BackendCarcassIdentity => GetGitProjectByKey($"{BackendCarcass}.{nameof(Identity)}");
    
    private const string Repositories = nameof(Repositories);
    public GitProjectDataDomain BackendCarcassRepositories => GetGitProjectByKey($"{BackendCarcass}.{nameof(Repositories)}");

    private const string Api = nameof(Api);
    public GitProjectDataDomain BackendCarcassApi => GetGitProjectByKey($"{BackendCarcass}.{nameof(Api)}");



    private const string AppCliTools = nameof(AppCliTools);

    private const string CliParameters = nameof(CliParameters);
    public GitProjectDataDomain AppCliToolsCliParameters => GetGitProjectByKey($"{AppCliTools}.{nameof(CliParameters)}");
    public GitProjectDataDomain CliTools => GetGitProjectByKey($"{AppCliTools}.{nameof(CliTools)}");
    public GitProjectDataDomain CliParametersDataEdit => GetGitProjectByKey($"{AppCliTools}.{nameof(CliParametersDataEdit)}");
    public GitProjectDataDomain CliToolsData => GetGitProjectByKey($"{AppCliTools}.{nameof(CliToolsData)}");

    private const string DbContextAnalyzer = nameof(DbContextAnalyzer);
    public GitProjectDataDomain AppCliToolsDbContextAnalyzer => GetGitProjectByKey($"{AppCliTools}.{nameof(DbContextAnalyzer)}");



    private const string DatabaseTools = nameof(DatabaseTools);

    public GitProjectDataDomain DbTools => GetGitProjectByKey($"{DatabaseTools}.{nameof(DbTools)}");



    private const string SystemTools = nameof(SystemTools);

    public GitProjectDataDomain SystemToolsShared => GetGitProjectByKey($"{SystemTools}.{nameof(SystemToolsShared)}");



    private const string WebSystemTools = nameof(WebSystemTools);

    public GitProjectDataDomain ApiExceptionHandler => GetGitProjectByKey(nameof(ApiExceptionHandler));

    //public GitProjectDataDomain StaticFilesTools => GetGitProjectByKey($"{SystemTools}.{nameof(StaticFilesTools)}");
    public GitProjectDataDomain TestToolsApi => GetGitProjectByKey(nameof(TestToolsApi));
    //public GitProjectDataDomain WebInstallers => GetGitProjectByKey($"{SystemTools}.{nameof(WebInstallers)}");
    public GitProjectDataDomain ConfigurationEncrypt => GetGitProjectByKey(nameof(ConfigurationEncrypt));
    public GitProjectDataDomain SerilogLogger => GetGitProjectByKey(nameof(SerilogLogger));
    public GitProjectDataDomain SwaggerTools => GetGitProjectByKey(nameof(SwaggerTools));
    public GitProjectDataDomain WindowsServiceTools => GetGitProjectByKey(nameof(WindowsServiceTools));
    public GitProjectDataDomain SignalRMessages => GetGitProjectByKey(nameof(SignalRMessages));
    public GitProjectDataDomain FluentValidationInstaller => GetGitProjectByKey(nameof(FluentValidationInstaller));

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

            if (string.IsNullOrWhiteSpace(value.ProjectFileName))
            {
                logger?.LogError("ProjectFileName is empty for Git Project with key {key})", key);
                continue;
            }

            var dependsOnProjectNames = value.DependsOnProjectNames;

            gitProjects.Add(key,
                new GitProjectDataDomain(value.GitName, value.ProjectRelativePath, value.ProjectFileName,
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