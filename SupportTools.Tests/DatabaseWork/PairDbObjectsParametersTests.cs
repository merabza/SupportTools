using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.DatabaseWork;

//ProdCopy ძველი MariaDB ბაზაა (errata), Dev — SQL Server
public sealed class PairDbObjectsParametersTests
{
    private const string ProjectName = "Errata";
    private const string MariaDbConnectionName = "MariaDbLocal";
    private const string SqlServerConnectionName = "SqlServerLocal";
    private readonly SupportToolsParameters _parameters = new();

    public PairDbObjectsParametersTests()
    {
        _parameters.DatabaseServerConnections[MariaDbConnectionName] = new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.MariaDb,
            ServerAddress = "localhost:3307",
            ServerUser = "root",
            ServerPass = "fake-password",
            ConnectionTimeOut = 30
        };
        _parameters.DatabaseServerConnections[SqlServerConnectionName] = new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.SqlServer,
            ServerAddress = "(local)",
            WindowsNtIntegratedSecurity = true
        };
    }

    [Fact]
    public void Create_WithMariaDbProdCopy_BuildsMariaDbConnectionStringForTheProdCopyDatabase()
    {
        // Arrange
        AddProject(MariaDbConnectionName);

        // Act
        var parameters = PairDbObjectsParameters.Create(NullLogger.Instance, _parameters, ProjectName);

        // Assert
        Assert.NotNull(parameters);
        Assert.Equal(EDatabaseProvider.MariaDb, parameters.ProdCopyDataProvider);
        var builder = new MySqlConnectionStringBuilder(parameters.ProdCopyConnectionString);
        Assert.Equal("localhost", builder.Server);
        Assert.Equal(3307u, builder.Port);
        Assert.Equal("root", builder.UserID);
        Assert.Equal("fake-password", builder.Password);
        Assert.Equal("ErrataDB", builder.Database);
        Assert.Equal(30u, builder.ConnectionTimeout);
        Assert.Equal(EDatabaseProvider.SqlServer, parameters.DevDataProvider);
    }

    //პორტის გარეშე MariaDB-ის ნაგულისხმევი 3306 რჩება; 0 დრო ბიბლიოთეკის ნაგულისხმევ 15 წამს ტოვებს
    [Fact]
    public void Create_WithMariaDbAddressWithoutPort_KeepsTheDefaultPortAndTimeout()
    {
        // Arrange
        DatabaseServerConnectionData connection = _parameters.DatabaseServerConnections[MariaDbConnectionName];
        connection.ServerAddress = "localhost";
        connection.ConnectionTimeOut = 0;
        AddProject(MariaDbConnectionName);

        // Act
        var parameters = PairDbObjectsParameters.Create(NullLogger.Instance, _parameters, ProjectName);

        // Assert
        Assert.NotNull(parameters);
        var builder = new MySqlConnectionStringBuilder(parameters.ProdCopyConnectionString);
        Assert.Equal("localhost", builder.Server);
        Assert.Equal(3306u, builder.Port);
        Assert.Equal(15u, builder.ConnectionTimeout);
    }

    //IPv6 მისამართის ორწერტილები პორტად არ იჭრება
    [Fact]
    public void Create_WithMariaDbIpv6Address_KeepsTheWholeAddressAsServer()
    {
        // Arrange
        _parameters.DatabaseServerConnections[MariaDbConnectionName].ServerAddress = "::1";
        AddProject(MariaDbConnectionName);

        // Act
        var parameters = PairDbObjectsParameters.Create(NullLogger.Instance, _parameters, ProjectName);

        // Assert
        Assert.NotNull(parameters);
        var builder = new MySqlConnectionStringBuilder(parameters.ProdCopyConnectionString);
        Assert.Equal("::1", builder.Server);
        Assert.Equal(3306u, builder.Port);
    }

    private void AddProject(string prodCopyConnectionName)
    {
        _parameters.Projects[ProjectName] = new ProjectModel
        {
            ProdCopyDatabaseParameters =
                new DatabaseParameters { DbConnectionName = prodCopyConnectionName, DatabaseName = "ErrataDB" },
            DevDatabaseParameters =
                new DatabaseParameters { DbConnectionName = SqlServerConnectionName, DatabaseName = "ErrataDev" },
            PairedDbObjectsResultFileName = @"D:\Errata\PairedDbObjects.json"
        };
    }
}
