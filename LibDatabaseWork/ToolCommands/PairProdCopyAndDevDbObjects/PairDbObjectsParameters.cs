using AppCliTools.CliParametersDataEdit;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairDbObjectsParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private PairDbObjectsParameters(EDatabaseProvider prodCopyDataProvider, string prodCopyConnectionString,
        EDatabaseProvider devDataProvider, string devConnectionString, int commandTimeOut, string resultFileName)
    {
        ProdCopyDataProvider = prodCopyDataProvider;
        ProdCopyConnectionString = prodCopyConnectionString;
        DevDataProvider = devDataProvider;
        DevConnectionString = devConnectionString;
        CommandTimeOut = commandTimeOut;
        ResultFileName = resultFileName;
    }

    public EDatabaseProvider ProdCopyDataProvider { get; }
    public string ProdCopyConnectionString { get; }
    public EDatabaseProvider DevDataProvider { get; }
    public string DevConnectionString { get; }
    public int CommandTimeOut { get; }
    public string ResultFileName { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static PairDbObjectsParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName)
    {
        ProjectModel? project = supportToolsParameters.GetProject(projectName);

        if (project is null)
        {
            logger.LogError("Project with name {ProjectName} not found", projectName);
            return null;
        }

        if (project.ProdCopyDatabaseParameters is null)
        {
            logger.LogError("Project with name {ProjectName} does not contains ProdCopyDatabaseParameters",
                projectName);
            return null;
        }

        if (project.DevDatabaseParameters is null)
        {
            logger.LogError("Project with name {ProjectName} does not contains DevDatabaseParameters", projectName);
            return null;
        }

        if (string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName))
        {
            logger.LogError("Project with name {ProjectName} does not contains PairedDbObjectsResultFileName",
                projectName);
            return null;
        }

        var databaseServerConnections = new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);

        (EDatabaseProvider? prodCopyDataProvider, string? prodCopyConnectionString, int prodCopyCommandTimeout) =
            DbConnectionFactory.GetDataProviderConnectionStringCommandTimeOut(project.ProdCopyDatabaseParameters,
                databaseServerConnections);

        if (prodCopyDataProvider is null || prodCopyConnectionString is null)
        {
            logger.LogError("could not Create ProdCopy Connection String for Project with name {ProjectName}",
                projectName);
            return null;
        }

        (EDatabaseProvider? devDataProvider, string? devConnectionString, int devCommandTimeout) =
            DbConnectionFactory.GetDataProviderConnectionStringCommandTimeOut(project.DevDatabaseParameters,
                databaseServerConnections);

        if (devDataProvider is null || devConnectionString is null)
        {
            logger.LogError("could not Create Dev Connection String for Project with name {ProjectName}", projectName);
            return null;
        }

        return new PairDbObjectsParameters(prodCopyDataProvider.Value, prodCopyConnectionString, devDataProvider.Value,
            devConnectionString,
            prodCopyCommandTimeout > devCommandTimeout ? prodCopyCommandTimeout : devCommandTimeout,
            project.PairedDbObjectsResultFileName);
    }
}
