//Created by ProjectParametersClassCreator at 5/9/2021 13:38:34

using AppCliTools.CliParametersDataEdit;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.Models;

public sealed class CorrectNewDbParameters : IParameters
{
    private CorrectNewDbParameters(EDatabaseProvider dataProvider, string connectionString, int commandTimeOut)
    {
        DataProvider = dataProvider;
        ConnectionString = connectionString;
        CommandTimeOut = commandTimeOut;
    }

    public EDatabaseProvider DataProvider { get; }
    public string ConnectionString { get; }
    public int CommandTimeOut { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static CorrectNewDbParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName)
    {
        ProjectModel? project = supportToolsParameters.GetProject(projectName);

        if (project is null)
        {
            logger.LogError("Project with name {ProjectName} not found", projectName);
            return null;
        }

        if (project.DevDatabaseParameters is null)
        {
            logger.LogError("Project with name {ProjectName} does not contains DevDatabaseConnectionParameters",
                projectName);
            return null;
        }

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);

        (EDatabaseProvider? devDataProvider, string? devConnectionString, int commandTimeout) =
            DbConnectionFactory.GetDataProviderConnectionStringCommandTimeOut(project.DevDatabaseParameters,
                databaseServerConnections);

        if (devDataProvider is null || devConnectionString is null)
        {
            logger.LogError("could not Created Connection String form Project with name {ProjectName}", projectName);
            return null;
        }

        var correctNewDbParameters = new CorrectNewDbParameters(devDataProvider.Value, devConnectionString,
            commandTimeout);

        return correctNewDbParameters;
    }
}
