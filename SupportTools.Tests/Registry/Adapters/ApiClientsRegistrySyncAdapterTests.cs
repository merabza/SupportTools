using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using ParametersManagement.LibApiClientParameters;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the ApiClient that SupportToolsServerWebApiClientName names belongs to this computer (G6, bootstrap)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ApiClientsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = "apiclients";
    private const string BootstrapName = "SupportToolsServer";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly ApiClientsRegistrySyncAdapter _sut;

    public ApiClientsRegistrySyncAdapterTests()
    {
        _context.Parameters.SupportToolsServerWebApiClientName = BootstrapName;
        _context.Parameters.ApiClients[BootstrapName] =
            new ApiClientSettings { Server = "http://sts.example.test/api/v1", ApiKey = "fake-bootstrap-key" };
        _context.Parameters.ApiClients["Pc1.WebAgent"] =
            new ApiClientSettings { Server = "http://pc1.example.test/api", ApiKey = "fake-agent-key" };
        _sut = new ApiClientsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void GetLocalRecords_WhenBootstrapApiClientExists_LeavesItOut()
    {
        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(["Pc1.WebAgent"], result.Keys);
    }

    //another computer may have uploaded a record with the name of this computer's bootstrap client
    [Fact]
    public async Task GetServerRecords_WhenServerHasRecordWithBootstrapNameInOtherCase_LeavesItOut()
    {
        // Arrange
        _context.Server.Store(Area, "supporttoolsserver", new StsApiClientDataModel { Name = "supporttoolsserver" },
            1);
        _context.Server.Store(Area, "Pc1.WebAgent", new StsApiClientDataModel { Name = "Pc1.WebAgent" }, 1);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(["Pc1.WebAgent"], result.Value.Keys);
    }

    [Fact]
    public void GetLocalRecords_WhenBootstrapNameIsNotSet_SyncsEveryApiClient()
    {
        // Arrange
        _context.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyLocal_WhenRecordExists_UpdatesTheSameObject()
    {
        // Arrange
        ApiClientSettings existing = _context.Parameters.ApiClients["Pc1.WebAgent"];

        // Act
        _sut.ApplyLocal("Pc1.WebAgent",
            new StsApiClientDataModel { Name = "Pc1.WebAgent", Server = "http://new.example.test/api" });

        // Assert
        Assert.Same(existing, _context.Parameters.ApiClients["Pc1.WebAgent"]);
        Assert.Equal("http://new.example.test/api", existing.Server);
        Assert.Null(existing.ApiKey);
    }
}
