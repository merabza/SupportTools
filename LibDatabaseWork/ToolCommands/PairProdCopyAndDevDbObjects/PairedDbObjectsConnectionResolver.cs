using AppCliTools.CliParametersDataEdit;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

//პროექტიდან ProdCopy და Dev ბაზების კავშირის სტრიქონების ამოღების დამხმარე
public sealed class PairedDbObjectsConnectionResolver
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedDbObjectsConnectionResolver(string prodCopyConnectionString, string devConnectionString)
    {
        ProdCopyConnectionString = prodCopyConnectionString;
        DevConnectionString = devConnectionString;
    }

    public string ProdCopyConnectionString { get; }
    public string DevConnectionString { get; }

    public static PairedDbObjectsConnectionResolver? Create(SupportToolsParameters supportToolsParameters,
        ProjectModel project, ILogger logger)
    {
        if (project.ProdCopyDatabaseParameters is null)
        {
            StShared.WriteErrorLine("Project does not contain ProdCopyDatabaseParameters", true, logger);
            return null;
        }

        if (project.DevDatabaseParameters is null)
        {
            StShared.WriteErrorLine("Project does not contain DevDatabaseParameters", true, logger);
            return null;
        }

        var databaseServerConnections =
            new DatabaseServerConnections(supportToolsParameters.DatabaseServerConnections);

        (EDatabaseProvider? prodCopyDataProvider, string? prodCopyConnectionString, int _) =
            DbConnectionFactory.GetDataProviderConnectionStringCommandTimeOut(project.ProdCopyDatabaseParameters,
                databaseServerConnections);

        if (prodCopyDataProvider is null || string.IsNullOrWhiteSpace(prodCopyConnectionString))
        {
            StShared.WriteErrorLine("Could not create ProdCopy connection string", true, logger);
            return null;
        }

        (EDatabaseProvider? devDataProvider, string? devConnectionString, int _) =
            DbConnectionFactory.GetDataProviderConnectionStringCommandTimeOut(project.DevDatabaseParameters,
                databaseServerConnections);

        if (devDataProvider is null || string.IsNullOrWhiteSpace(devConnectionString))
        {
            StShared.WriteErrorLine("Could not create Dev connection string", true, logger);
            return null;
        }

        return new PairedDbObjectsConnectionResolver(prodCopyConnectionString, devConnectionString);
    }
}
