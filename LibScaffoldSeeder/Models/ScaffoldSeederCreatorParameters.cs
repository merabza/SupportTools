using System;
using DbTools;
using LibAppProjectCreator.Models;
using LibFileParameters.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibScaffoldSeeder.Models;

public sealed class ScaffoldSeederCreatorParameters : IParameters
{
    private ScaffoldSeederCreatorParameters(string logFolder, string scaffoldSeedersWorkFolder, string projectName,
        string scaffoldSeederProjectName, string projectSecurityFolderPath, string projectShortPrefix,
        string mainDatabaseProjectName, string projectDbContextClassName, EDataProvider devDatabaseDataProvider,
        string devDatabaseConnectionString, EDataProvider prodCopyDatabaseDataProvider,
        string prodCopyDatabaseConnectionString, string newDataSeedingClassLibProjectName,
        SmartSchema smartSchemaForLocal, string excludesRulesParametersFilePath, string fakeHostProjectName,
        string? migrationSqlFilesFolder, GitProjects gitProjects, GitRepos gitRepos)
    {
        LogFolder = logFolder;
        ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;
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
    }

    public string LogFolder { get; }
    public string ScaffoldSeedersWorkFolder { get; }
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

    //public SmartSchemas SmartSchemas { get; set; }
    public GitProjects GitProjects { get; }
    public GitRepos GitRepos { get; }
    public string ExcludesRulesParametersFilePath { get; }
    public string? MigrationSqlFilesFolder { get; }
    public string CreateProjectSeederCodeProjectName => $"Create{ScaffoldSeederProjectName}SeederCode";
    public string GetJsonFromScaffoldDbProjectName => $"GetJsonFromScaffold{MainDatabaseProjectName}";
    public string SeedDbProjectName => $"Seed{MainDatabaseProjectName}";

    //public string FakeHostProjectName => "FakeHost";
    public string FakeHostProjectName { get; }

    public string DataSeedingClassLibProjectName => $"{MainDatabaseProjectName}DataSeeding";
    public string DbMigrationProjectName => $"{MainDatabaseProjectName}Migration";

    public bool CheckBeforeSave()
    {
        return true;
    }


    public static ScaffoldSeederCreatorParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName)
    {
        //SettingsGetter settingsGetter = new SettingsGetter(supportToolsParameters, projectName, null);
        //if (!settingsGetter.Run(false, false, false, true, false))
        //    return null;

        //ProjectModel project = settingsGetter.Project;

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

            //if (string.IsNullOrWhiteSpace(project.MigrationSqlFilesFolder))
            //{
            //    StShared.WriteErrorLine($"MigrationSqlFilesFolder does not specified for Project {projectName}", true);
            //    return null;
            //}

            var gitProjects = GitProjects.Create(logger, supportToolsParameters.GitProjects);
            var scaffoldSeederCreatorParameters = new ScaffoldSeederCreatorParameters(supportToolsParameters.LogFolder,
                supportToolsParameters.ScaffoldSeedersWorkFolder, projectName, project.ScaffoldSeederProjectName,
                project.ProjectSecurityFolderPath, project.ProjectShortPrefix, project.DbContextProjectName,
                project.DbContextName, project.DevDatabaseConnectionParameters.DataProvider,
                project.DevDatabaseConnectionParameters.ConnectionString,
                project.DevDatabaseConnectionParameters.DataProvider,
                project.ProdCopyDatabaseConnectionParameters.ConnectionString,
                project.NewDataSeedingClassLibProjectName, smartSchemaForLocal, project.ExcludesRulesParametersFilePath,
                supportToolsParameters.AppProjectCreatorAllParameters.FakeHostProjectName,
                project.MigrationSqlFilesFolder, gitProjects,
                GitRepos.Create(logger, supportToolsParameters.Gits, project.MainProjectFolderRelativePath(gitProjects),
                    project.SpaProjectFolderRelativePath(gitProjects)));
            return scaffoldSeederCreatorParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}