using System;
using System.Collections.Generic;
using DbTools;
using LibFileParameters.Models;
using LibGitData.Models;
using LibGitWork;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibScaffoldSeeder.Models;

public sealed class ScaffoldSeederCreatorParameters : IParameters
{
    private ScaffoldSeederCreatorParameters(string logFolder, string scaffoldSeedersWorkFolder, string tempFolder,
        string projectName,
        string scaffoldSeederProjectName, string projectSecurityFolderPath, string projectShortPrefix,
        string mainDatabaseProjectName, string projectDbContextClassName, EDataProvider devDatabaseDataProvider,
        string devDatabaseConnectionString, EDataProvider prodCopyDatabaseDataProvider,
        string prodCopyDatabaseConnectionString, string newDataSeedingClassLibProjectName,
        SmartSchema smartSchemaForLocal, string excludesRulesParametersFilePath, string fakeHostProjectName,
        string? migrationSqlFilesFolder, GitProjects gitProjects, GitRepos gitRepos,
        Dictionary<string, string> gitIgnoreModelFilePaths)
    {
        LogFolder = logFolder;
        ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;
        TempFolder = tempFolder;
        ProjectName = projectName;
        ScaffoldSeederProjectName = scaffoldSeederProjectName;
        ProjectSecurityFolderPath = projectSecurityFolderPath;
        ProjectShortPrefix = projectShortPrefix;
        MainDatabaseProjectName = mainDatabaseProjectName;
        ProjectDbContextClassName = projectDbContextClassName;
        DevDatabaseDataProvider = devDatabaseDataProvider;
        DevDatabaseConnectionString = devDatabaseConnectionString;
        ProdCopyDatabaseDataProvider = prodCopyDatabaseDataProvider;
        ProdCopyDatabaseConnectionString = prodCopyDatabaseConnectionString;
        NewDataSeedingClassLibProjectName = newDataSeedingClassLibProjectName;
        SmartSchemaForLocal = smartSchemaForLocal;
        ExcludesRulesParametersFilePath = excludesRulesParametersFilePath;
        FakeHostProjectName = fakeHostProjectName;
        MigrationSqlFilesFolder = migrationSqlFilesFolder;
        GitProjects = gitProjects;
        GitRepos = gitRepos;
        GitIgnoreModelFilePaths = gitIgnoreModelFilePaths;
    }

    public string LogFolder { get; }
    public string ScaffoldSeedersWorkFolder { get; }
    public string TempFolder { get; }
    public string ProjectName { get; }
    public string ScaffoldSeederProjectName { get; }
    public string ProjectSecurityFolderPath { get; }
    public string ProjectShortPrefix { get; }
    public string MainDatabaseProjectName { get; }
    public string ProjectDbContextClassName { get; }
    public EDataProvider DevDatabaseDataProvider { get; }
    public string DevDatabaseConnectionString { get; }
    public EDataProvider ProdCopyDatabaseDataProvider { get; }
    public string ProdCopyDatabaseConnectionString { get; }
    public string NewDataSeedingClassLibProjectName { get; }
    public SmartSchema SmartSchemaForLocal { get; }
    public GitProjects GitProjects { get; }
    public GitRepos GitRepos { get; }
    public Dictionary<string, string> GitIgnoreModelFilePaths { get; }
    public string ExcludesRulesParametersFilePath { get; }
    public string? MigrationSqlFilesFolder { get; }
    public string CreateProjectSeederCodeProjectName => $"Create{ScaffoldSeederProjectName}SeederCode";
    public string GetJsonFromScaffoldDbProjectName => $"GetJsonFromScaffold{MainDatabaseProjectName}";
    public string SeedDbProjectName => $"Seed{MainDatabaseProjectName}";
    public string FakeHostProjectName { get; }
    public string DataSeedingClassLibProjectName => $"{MainDatabaseProjectName}DataSeeding";
    public string DbMigrationProjectName => $"{MainDatabaseProjectName}Migration";

    public bool CheckBeforeSave()
    {
        return true;
    }


    public static ScaffoldSeederCreatorParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName, bool useConsole)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(supportToolsParameters.LogFolder))
            {
                StShared.WriteErrorLine("LogFolder does not specified in parameters", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
            {
                StShared.WriteErrorLine("ScaffoldSeedersWorkFolder does not specified in parameters", true);
                return null;
            }


            if (string.IsNullOrWhiteSpace(supportToolsParameters.TempFolder))
            {
                StShared.WriteErrorLine("TempFolder does not specified in parameters", true);
                return null;
            }

            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (string.IsNullOrWhiteSpace(supportToolsParameters.SmartSchemaNameForLocal))
            {
                StShared.WriteErrorLine($"SmartSchemaNameForLocal does not specified for Project {projectName}", true);
                return null;
            }

            var smartSchemaForLocal =
                supportToolsParameters.GetSmartSchemaRequired(supportToolsParameters.SmartSchemaNameForLocal);

            if (string.IsNullOrWhiteSpace(project.ProjectSecurityFolderPath))
            {
                StShared.WriteErrorLine($"ProjectSecurityFolderPath does not specified for Project {projectName}",
                    true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.ProjectShortPrefix))
            {
                StShared.WriteErrorLine($"ProjectShortPrefix does not specified for Project {projectName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
            {
                StShared.WriteErrorLine($"ScaffoldSeederProjectName does not specified for Project {projectName}",
                    true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.DbContextProjectName))
            {
                StShared.WriteErrorLine($"DbContextProjectName does not specified for Project {projectName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.DbContextName))
            {
                StShared.WriteErrorLine($"DbContextName does not specified for Project {projectName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.DevDatabaseConnectionParameters?.ConnectionString))
            {
                StShared.WriteErrorLine(
                    $"DevDatabaseConnectionParameters.ConnectionString does not specified for Project {projectName}",
                    true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.ProdCopyDatabaseConnectionParameters?.ConnectionString))
            {
                StShared.WriteErrorLine(
                    $"ProdCopyDatabaseConnectionParameters.ConnectionString does not specified for Project {projectName}",
                    true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.NewDataSeedingClassLibProjectName))
            {
                StShared.WriteErrorLine(
                    $"NewDataSeedingClassLibProjectName does not specified for Project {projectName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.ExcludesRulesParametersFilePath))
            {
                StShared.WriteErrorLine($"ExcludesRulesParametersFilePath does not specified for Project {projectName}",
                    true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(supportToolsParameters.AppProjectCreatorAllParameters?.FakeHostProjectName))
            {
                StShared.WriteErrorLine(
                    "supportToolsParameters.AppProjectCreatorAllParameters.FakeHostProjectName does not specified",
                    true);
                return null;
            }

            var gitProjects = GitProjects.Create(logger, supportToolsParameters.GitProjects);
            var scaffoldSeederCreatorParameters = new ScaffoldSeederCreatorParameters(supportToolsParameters.LogFolder,
                supportToolsParameters.ScaffoldSeedersWorkFolder, supportToolsParameters.TempFolder, projectName,
                project.ScaffoldSeederProjectName,
                project.ProjectSecurityFolderPath, project.ProjectShortPrefix, project.DbContextProjectName,
                project.DbContextName, project.DevDatabaseConnectionParameters.DataProvider,
                project.DevDatabaseConnectionParameters.ConnectionString,
                project.DevDatabaseConnectionParameters.DataProvider,
                project.ProdCopyDatabaseConnectionParameters.ConnectionString,
                project.NewDataSeedingClassLibProjectName, smartSchemaForLocal, project.ExcludesRulesParametersFilePath,
                supportToolsParameters.AppProjectCreatorAllParameters.FakeHostProjectName,
                project.MigrationSqlFilesFolder, gitProjects,
                GitRepos.Create(logger, supportToolsParameters.Gits, project.SpaProjectFolderRelativePath(gitProjects),
                    useConsole, false),
                supportToolsParameters.GitIgnoreModelFilePaths);
            return scaffoldSeederCreatorParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}