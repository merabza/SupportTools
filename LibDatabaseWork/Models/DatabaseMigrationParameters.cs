using System.Net.Http;
using DatabasesManagement;
using DatabasesManagement.Errors;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace LibDatabaseWork.Models;

public sealed class DatabaseMigrationParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private DatabaseMigrationParameters(string migrationStartupProjectFilePath, string migrationProjectFileName,
        string dbContextName, string solutionFileNameWithMigrationProject, IDatabaseManager databaseManager,
        string databaseName)
    {
        MigrationStartupProjectFilePath = migrationStartupProjectFilePath;
        MigrationProjectFileName = migrationProjectFileName;
        DbContextName = dbContextName;
        SolutionFileNameWithMigrationProject = solutionFileNameWithMigrationProject;
        DatabaseManager = databaseManager;
        DatabaseName = databaseName;
    }

    public string MigrationStartupProjectFilePath { get; set; }
    public string MigrationProjectFileName { get; set; }
    public string DbContextName { get; set; }
    public string SolutionFileNameWithMigrationProject { get; }
    public IDatabaseManager DatabaseManager { get; }
    public string DatabaseName { get; }


    public bool CheckBeforeSave()
    {
        return true;
    }

    public static DatabaseMigrationParameters? Create(ILogger logger, IHttpClientFactory httpClientFactory,
        SupportToolsParameters supportToolsParameters, string projectName)
    {
        var project = supportToolsParameters.GetProject(projectName);

        //პროექტების მიხედვით სწორი პროექტის ფაილის დადგენა მომავლისთვის გადავდე
        //GitProjects gitProjects = new GitProjects(supportToolsParameters.GitProjects);
        //GitProjectDataModel gitProject = gitProjects.GetGitProjectByKey(projectName);
        //StartupProjectFileName = Path.Combine(supportToolsParameters.ScaffoldSeedersWorkFolder, projectName, project.MigrationStartupProjectFilePath),//gitProject.ProjectRelativePath

        if (project is null)
        {
            logger.LogError("Project with name {projectName} not found", projectName);
            return null;
        }

        var solutionFileNameWithMigrationProject =
            project.SolutionFileNameWithMigrationProject ?? project.SolutionFileName;
        if (solutionFileNameWithMigrationProject is null)
        {
            logger.LogError(
                "Project with name {projectName} does not contains SolutionFileNameWithMigrationProject and SolutionFileName",
                projectName);
            return null;
        }

        if (project.MigrationStartupProjectFilePath is null)
        {
            logger.LogError("Project with name {projectName} does not contains MigrationStartupProjectFilePath",
                projectName);
            return null;
        }

        if (project.MigrationProjectFilePath is null)
        {
            logger.LogError("Project with name {projectName} does not contains MigrationProjectFilePath", projectName);
            return null;
        }

        if (project.DbContextName is null)
        {
            logger.LogError("Project with name {projectName} does not contains DbContextName", projectName);
            return null;
        }

        var devDatabaseParameters = project.DevDatabaseParameters;

        if (devDatabaseParameters is null)
        {
            logger.LogError("DevDatabaseParameters is not specified for Project {projectName}", projectName);
            return null;
        }

        if (string.IsNullOrWhiteSpace(devDatabaseParameters.DatabaseName))
        {
            logger.LogError("DatabaseName is not specified for Project {projectName}", projectName);
            return null;
        }

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);
        var apiClients = new ApiClients(supportToolsParameters.ApiClients);

        var createDatabaseManagerResult = DatabaseManagersFabric.CreateDatabaseManager(logger, true,
            devDatabaseParameters.DbConnectionName, databaseServerConnections, apiClients, httpClientFactory, null,
            null).Result;
        if (createDatabaseManagerResult.IsT1)
        {
            logger.LogError(DatabaseManagerErrors.CanNotCreateDatabaseServerClient.ErrorMessage);
            return null;
        }

        var databaseMigrationParameters = new DatabaseMigrationParameters(project.MigrationStartupProjectFilePath,
            project.MigrationProjectFilePath, project.DbContextName, solutionFileNameWithMigrationProject,
            createDatabaseManagerResult.AsT0, devDatabaseParameters.DatabaseName);

        return databaseMigrationParameters;
    }
}