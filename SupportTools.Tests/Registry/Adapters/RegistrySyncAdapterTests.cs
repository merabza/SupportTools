using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using ParametersManagement.LibApiClientParameters;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the base of every adapter: the keys of the last server read (IRegistryServerKeys), which the Projects adapter checks
//its references against. They hold every server record, also the ones the sync leaves out
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RegistrySyncAdapterTests : IDisposable
{
    private const string ApiClientsArea = "apiclients";
    private const string ConnectionsArea = "databaseserverconnections";
    private const string BootstrapName = "SupportToolsServer";
    private readonly RegistryAdapterTestContext _context = new();

    public RegistrySyncAdapterTests()
    {
        _context.Parameters.SupportToolsServerWebApiClientName = BootstrapName;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void ServerKeys_WhenServerWasNotRead_IsEmpty()
    {
        // Arrange
        var sut = new ApiClientsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);

        // Act
        IReadOnlyCollection<string> result = sut.ServerKeys;

        // Assert
        Assert.Empty(result);
    }

    //the bootstrap ApiClient is not synced, but it exists on the server
    [Fact]
    public async Task ServerKeys_WhenARecordIsNotSynced_StillHoldsItsKey()
    {
        // Arrange
        _context.Server.Store(ApiClientsArea, BootstrapName, new StsApiClientDataModel { Name = BootstrapName }, 1);
        _context.Server.Store(ApiClientsArea, "Pc1.WebAgent", new StsApiClientDataModel { Name = "Pc1.WebAgent" }, 1);
        var sut = new ApiClientsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);

        // Act
        await sut.GetServerRecords(default);

        // Assert
        Assert.Equal(["Pc1.WebAgent", BootstrapName], sut.ServerKeys.Order(StringComparer.Ordinal));
    }

    //a record with a value unknown to this client is left out of the sync, but it exists on the server
    [Fact]
    public async Task ServerKeys_WhenARecordIsUnsupported_StillHoldsItsKey()
    {
        // Arrange
        _context.Server.Store(ConnectionsArea, "Dev",
            new StsDatabaseServerConnectionDataModel { Name = "Dev", DatabaseServerProvider = "Oracle" }, 1);
        var sut = new DatabaseServerConnectionsRegistrySyncAdapter(_context.ApiClient, _context.Parameters,
            _context.Warnings);

        // Act
        await sut.GetServerRecords(default);

        // Assert
        Assert.Equal(["Dev"], sut.ServerKeys);
        Assert.Single(_context.Warnings.Items);
    }

    //the keys match ignoring case, like the records (G8)
    [Fact]
    public async Task ServerKeys_WhenRead_MatchIgnoringCase()
    {
        // Arrange
        _context.Server.Store(ApiClientsArea, "Pc1.WebAgent", new StsApiClientDataModel { Name = "Pc1.WebAgent" }, 1);
        var sut = new ApiClientsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);

        // Act
        await sut.GetServerRecords(default);

        // Assert
        Assert.Contains("pc1.webagent", sut.ServerKeys);
    }

    //every read replaces the keys of the previous one
    [Fact]
    public async Task ServerKeys_WhenServerIsReadAgain_HoldOnlyTheKeysOfTheLastRead()
    {
        // Arrange
        _context.Server.Store(ApiClientsArea, "Pc1.WebAgent", new StsApiClientDataModel { Name = "Pc1.WebAgent" }, 1);
        var sut = new ApiClientsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
        await sut.GetServerRecords(default);
        _context.Server.Records(ApiClientsArea).Clear();
        _context.Server.Store(ApiClientsArea, "Pc2.WebAgent", new StsApiClientDataModel { Name = "Pc2.WebAgent" }, 1);

        // Act
        await sut.GetServerRecords(default);

        // Assert
        Assert.Equal(["Pc2.WebAgent"], sut.ServerKeys);
    }

    //the local records are not server keys
    [Fact]
    public void ServerKeys_WhenOnlyLocalRecordsExist_IsEmpty()
    {
        // Arrange
        _context.Parameters.ApiClients["Pc1.WebAgent"] = new ApiClientSettings { Server = "http://pc1.example.test" };
        var sut = new ApiClientsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);

        // Act
        sut.GetLocalRecords();

        // Assert
        Assert.Empty(sut.ServerKeys);
    }
}
