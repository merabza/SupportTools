using System;
using System.Collections.Generic;
using LibGitData.Domain;
using Microsoft.Extensions.Logging;

namespace LibGitData.Models;

public sealed class GitProjects
{
    //AppCliTools
    private const string AppCliTools = nameof(AppCliTools);
    private const string CliParameters = nameof(CliParameters);
    private const string CliParametersDataEdit = nameof(CliParametersDataEdit);
    private const string CliTools = nameof(CliTools);
    private const string CliToolsData = nameof(CliToolsData);
    private const string DbContextAnalyzer = nameof(DbContextAnalyzer);

    //BackendCarcass
    private const string BackendCarcass = nameof(BackendCarcass);
    private const string DataSeeding = nameof(DataSeeding);
    private const string Db = nameof(Db);

    //DatabaseTools
    private const string DatabaseTools = nameof(DatabaseTools);
    private const string DbTools = nameof(DbTools);

    //ParametersManagement
    private const string Api = nameof(Api);
    private const string ParametersManagement = nameof(ParametersManagement);
    private const string LibDatabaseParameters = nameof(LibDatabaseParameters);
    private const string Identity = nameof(Identity);
    private const string Repositories = nameof(Repositories);

    //SystemTools
    private const string SystemTools = nameof(SystemTools);
    private const string SystemToolsShared = nameof(SystemToolsShared);

    //WebSystemTools
    private const string WebSystemTools = nameof(WebSystemTools);
    private const string ApiExceptionHandler = nameof(ApiExceptionHandler);
    private const string ConfigurationEncrypt = nameof(ConfigurationEncrypt);
    private const string TestToolsApi = nameof(TestToolsApi);
    private const string SerilogLogger = nameof(SerilogLogger);
    private const string SwaggerTools = nameof(SwaggerTools);
    private const string WindowsServiceTools = nameof(WindowsServiceTools);
    private const string SignalRMessages = nameof(SignalRMessages);
    private const string ValidationTools = nameof(ValidationTools);

    private readonly Dictionary<string, GitProjectDataDomain> _gitProjects;

    private GitProjects(Dictionary<string, GitProjectDataDomain> gitProjects)
    {
        _gitProjects = gitProjects;
    }

    //AppCliTools
    public GitProjectDataDomain AppCliToolsCliParameters => GetGitProjectByKey($"{AppCliTools}.{nameof(CliParameters)}");
    public GitProjectDataDomain AppCliToolsCliTools => GetGitProjectByKey($"{AppCliTools}.{nameof(CliTools)}");
    public GitProjectDataDomain AppCliToolsCliParametersDataEdit => GetGitProjectByKey($"{AppCliTools}.{nameof(CliParametersDataEdit)}");
    public GitProjectDataDomain AppCliToolsCliToolsData => GetGitProjectByKey($"{AppCliTools}.{nameof(CliToolsData)}");
    public GitProjectDataDomain AppCliToolsDbContextAnalyzer => GetGitProjectByKey($"{AppCliTools}.{nameof(DbContextAnalyzer)}");

    //BackendCarcass
    public GitProjectDataDomain BackendCarcassApi => GetGitProjectByKey($"{BackendCarcass}.{nameof(Api)}");
    public GitProjectDataDomain BackendCarcassDb => GetGitProjectByKey($"{BackendCarcass}.{Db}");
    public GitProjectDataDomain BackendCarcassDataSeeding => GetGitProjectByKey($"{BackendCarcass}.{DataSeeding}");
    public GitProjectDataDomain BackendCarcassIdentity => GetGitProjectByKey($"{BackendCarcass}.{nameof(Identity)}");
    public GitProjectDataDomain BackendCarcassRepositories => GetGitProjectByKey($"{BackendCarcass}.{nameof(Repositories)}");

    //DatabaseTools
    public GitProjectDataDomain DatabaseToolsDbTools => GetGitProjectByKey($"{DatabaseTools}.{nameof(DbTools)}");

    //ParametersManagement
    public GitProjectDataDomain ParametersManagementLibDatabaseParameters => GetGitProjectByKey($"{ParametersManagement}.{LibDatabaseParameters}");

    //SystemTools
    public GitProjectDataDomain SystemToolsSystemToolsShared => GetGitProjectByKey($"{SystemTools}.{nameof(SystemToolsShared)}");

    //WebSystemTools
    public GitProjectDataDomain WebSystemToolsApiExceptionHandler => GetGitProjectByKey($"{WebSystemTools}.{nameof(ApiExceptionHandler)}");
    public GitProjectDataDomain WebSystemToolsConfigurationEncrypt => GetGitProjectByKey($"{WebSystemTools}.{nameof(ConfigurationEncrypt)}");
    public GitProjectDataDomain WebSystemToolsSerilogLogger => GetGitProjectByKey($"{WebSystemTools}.{nameof(SerilogLogger)}");
    public GitProjectDataDomain WebSystemToolsSignalRMessages => GetGitProjectByKey($"{WebSystemTools}.{nameof(SignalRMessages)}");
    public GitProjectDataDomain WebSystemToolsSwaggerTools => GetGitProjectByKey($"{WebSystemTools}.{nameof(SwaggerTools)}");
    public GitProjectDataDomain WebSystemToolsTestToolsApi => GetGitProjectByKey($"{WebSystemTools}.{nameof(TestToolsApi)}");
    public GitProjectDataDomain WebSystemToolsValidationTools => GetGitProjectByKey($"{WebSystemTools}.{nameof(ValidationTools)}");
    public GitProjectDataDomain WebSystemToolsWindowsServiceTools => GetGitProjectByKey($"{WebSystemTools}.{nameof(WindowsServiceTools)}");

    public static GitProjects Create(ILogger? logger, Dictionary<string, GitProjectDataModel> gitPrs)
    {
        Dictionary<string, GitProjectDataDomain> gitProjects = [];
        foreach ((string key, GitProjectDataModel value) in gitPrs)
        {
            if (string.IsNullOrWhiteSpace(value.GitName))
            {
                logger?.LogError("GitName is empty for Git Project with key {Key})", key);
                continue;
            }

            if (string.IsNullOrWhiteSpace(value.ProjectRelativePath))
            {
                logger?.LogError("ProjectRelativePath is empty for Git Project with key {Key})", key);
                continue;
            }

            if (string.IsNullOrWhiteSpace(value.ProjectFileName))
            {
                logger?.LogError("ProjectFileName is empty for Git Project with key {Key})", key);
                continue;
            }

            List<string> dependsOnProjectNames = value.DependsOnProjectNames;

            gitProjects.Add(key,
                new GitProjectDataDomain(value.GitName, value.ProjectRelativePath, value.ProjectFileName,
                    dependsOnProjectNames));
        }

        return new GitProjects(gitProjects);
    }

    public GitProjectDataDomain GetGitProjectByKey(string key)
    {
        return _gitProjects.TryGetValue(key, out GitProjectDataDomain? byKey)
            ? byKey
            : throw new Exception($"GitProject With Key {key} does not found");
    }

    public GitProjectDataDomain? GetGitProjectIfExistsByKey(string key)
    {
        return _gitProjects.GetValueOrDefault(key);
    }
}
