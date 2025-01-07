//Created by ProjectParametersClassCreator at 5/9/2021 13:38:34

using CliParametersDataEdit;
using DbTools;
using LibDatabaseParameters;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace LibDatabaseWork.Models;

public sealed class CorrectNewDbParameters : IParameters
{
    private CorrectNewDbParameters(EDataProvider dataProvider, string connectionString, int commandTimeOut)
    {
        DataProvider = dataProvider;
        ConnectionString = connectionString;
        CommandTimeOut = commandTimeOut;
    }

    public EDataProvider DataProvider { get; }
    public string ConnectionString { get; }
    public int CommandTimeOut { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static CorrectNewDbParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName)
    {
        var project = supportToolsParameters.GetProject(projectName);

        if (project is null)
        {
            logger.LogError("Project with name {projectName} not found", projectName);
            return null;
        }

        if (project.DevDatabaseParameters is null)
        {
            logger.LogError("Project with name {projectName} does not contains DevDatabaseConnectionParameters",
                projectName);
            return null;
        }

        DatabaseServerConnections databaseServerConnections = new(supportToolsParameters.DatabaseServerConnections);
        var connectionString =
            DbConnectionFabric.GetDbConnectionString(project.DevDatabaseParameters, databaseServerConnections);
        if (connectionString is null)
        {
            logger.LogError("could not Created Connection String form Project with name {projectName}", projectName);
            return null;
        }

        var correctNewDbParameters = new CorrectNewDbParameters(project.DevDatabaseParameters.DataProvider,
            connectionString, project.DevDatabaseParameters.CommandTimeOut);

        return correctNewDbParameters;
    }
}