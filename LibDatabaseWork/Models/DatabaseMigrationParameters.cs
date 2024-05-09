using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace LibDatabaseWork.Models;

public sealed class DatabaseMigrationParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private DatabaseMigrationParameters(string startupProjectFileName, string migrationProjectFileName,
        string dbContextName)
    {
        //ProjectName = projectName;
        StartupProjectFileName = startupProjectFileName;
        MigrationProjectFileName = migrationProjectFileName;
        DbContextName = dbContextName;
    }

    //public string ProjectName { get; set; }
    public string StartupProjectFileName { get; set; }
    public string MigrationProjectFileName { get; set; }
    public string DbContextName { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static DatabaseMigrationParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName)
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

        var databaseMigrationParameters =
            new DatabaseMigrationParameters(project.MigrationStartupProjectFilePath, project.MigrationProjectFilePath,
                project.DbContextName);

        return databaseMigrationParameters;
    }
}