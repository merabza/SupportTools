using System.Net.Http;
using Microsoft.Extensions.Logging;
using OneOf;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement;
using ToolsManagement.DatabasesManagement.Errors;

namespace LibDatabaseWork.Models;

public sealed class DatabaseMigrationParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private DatabaseMigrationParameters(string migrationStartupProjectFilePath, string migrationProjectFileName,
        string dbContextName, IDatabaseManager databaseManager, string databaseName)
    {
        MigrationStartupProjectFilePath = migrationStartupProjectFilePath;
        MigrationProjectFileName = migrationProjectFileName;
        DbContextName = dbContextName;
        //SolutionFileNameWithMigrationProject = solutionFileNameWithMigrationProject;
        DatabaseManager = databaseManager;
        DatabaseName = databaseName;
    }

    public string MigrationStartupProjectFilePath { get; set; }
    public string MigrationProjectFileName { get; set; }

    public string DbContextName { get; set; }

    //public string SolutionFileNameWithMigrationProject { get; }
    public IDatabaseManager DatabaseManager { get; }
    public string DatabaseName { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static DatabaseMigrationParameters? Create(ILogger logger, IHttpClientFactory httpClientFactory,
        SupportToolsParameters supportToolsParameters, string projectName)
    {
        ProjectModel? project = supportToolsParameters.GetProject(projectName);

        //პროექტების მიხედვით სწორი პროექტის ფაილის დადგენა მომავლისთვის გადავდე
        //GitProjects gitProjects = new GitProjects(supportToolsParameters.GitProjects);
        //GitProjectDataModel gitProject = gitProjects.GetGitProjectByKey(projectName);
        //StartupProjectFileName = Path.Combine(supportToolsParameters.ScaffoldSeedersWorkFolder, projectName, project.MigrationStartupProjectFilePath),//gitProject.ProjectRelativePath

        if (project is null)
        {
            logger.LogError("Project with name {ProjectName} not found", projectName);
            return null;
        }

        //var solutionFileNameWithMigrationProject = project.SolutionFileNameWithMigrationProject;
        //if (string.IsNullOrWhiteSpace(solutionFileNameWithMigrationProject))
        //    solutionFileNameWithMigrationProject = project.SolutionFileName;

        //if (solutionFileNameWithMigrationProject is null)
        //{
        //    logger.LogError(
        //        "Project with name {projectName} does not contains SolutionFileNameWithMigrationProject and SolutionFileName",
        //        projectName);
        //    return null;
        //}

        if (project.MigrationStartupProjectFilePath is null)
        {
            logger.LogError("Project with name {ProjectName} does not contains MigrationStartupProjectFilePath",
                projectName);
            return null;
        }

        if (project.MigrationProjectFilePath is null)
        {
            logger.LogError("Project with name {ProjectName} does not contains MigrationProjectFilePath", projectName);
            return null;
        }

        if (project.DbContextName is null)
        {
            logger.LogError("Project with name {ProjectName} does not contains DbContextName", projectName);
            return null;
        }

        DatabaseParameters? devDatabaseParameters = project.DevDatabaseParameters;

        if (devDatabaseParameters is null)
        {
            logger.LogError("DevDatabaseParameters is not specified for Project {ProjectName}", projectName);
            return null;
        }

        if (string.IsNullOrWhiteSpace(devDatabaseParameters.DatabaseName))
        {
            logger.LogError("DatabaseName is not specified for Project {ProjectName}", projectName);
            return null;
        }

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);
        var apiClients = new ApiClients(supportToolsParameters.ApiClients);

        OneOf<IDatabaseManager, Err[]> createDatabaseManagerResult = DatabaseManagersFactory
            .CreateDatabaseManager(logger, true, devDatabaseParameters.DbConnectionName, databaseServerConnections,
                apiClients, httpClientFactory, null, null).Result;
        if (createDatabaseManagerResult.IsT1)
        {
#pragma warning disable CA2254
            logger.LogError(DatabaseManagerErrors.CanNotCreateDatabaseServerClient.ErrorMessage);
#pragma warning restore CA2254
            return null;
        }

        var databaseMigrationParameters = new DatabaseMigrationParameters(project.MigrationStartupProjectFilePath,
            project.MigrationProjectFilePath, project.DbContextName, createDatabaseManagerResult.AsT0,
            devDatabaseParameters.DatabaseName);

        return databaseMigrationParameters;
    }
}
