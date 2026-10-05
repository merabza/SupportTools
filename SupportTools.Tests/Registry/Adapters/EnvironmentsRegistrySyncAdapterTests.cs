using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the template of the versioned registry areas (B1): the shared behavior of the adapters is tested here once
[Collection(ConsoleCaptureCollection.Name)]
public sealed class EnvironmentsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = "environments";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly EnvironmentsRegistrySyncAdapter _sut;

    public EnvironmentsRegistrySyncAdapterTests()
    {
        _sut = new EnvironmentsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues()
    {
        // Assert
        Assert.Equal(RegistryCollections.Environments, _sut.CollectionName);
        Assert.Equal(RegistryCollections.EnvironmentsOrder, _sut.Order);
    }

    [Fact]
    public void GetLocalRecords_WhenCalled_ReturnsContractsByLocalKey()
    {
        // Arrange
        _context.Parameters.Environments["Production"] = "Live";
        _context.Parameters.Environments["Development"] = "";

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(2, result.Count);
        var production = Assert.IsType<StsEnvironmentDataModel>(result["Production"]);
        Assert.Equal("Production", production.Name);
        Assert.Equal("Live", production.Description);
    }

    //the engine reports keys that differ only by case, so the adapter must not merge them
    [Fact]
    public void GetLocalRecords_WhenKeysDifferOnlyByCase_ReturnsBoth()
    {
        // Arrange
        _context.Parameters.Environments["Production"] = "Live";
        _context.Parameters.Environments["production"] = "Live";

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(["Production", "production"], result.Keys);
    }

    [Fact]
    public async Task GetServerRecords_WhenServerHasRecords_ReturnsThemWithTheirVersions()
    {
        // Arrange
        _context.Server.Store(Area, "Production", new StsEnvironmentDataModel { Name = "Production" }, 3);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.True(result.IsSuccess);
        RegistryServerRecord record = result.Value["Production"];
        Assert.Equal(3, record.Version);
        Assert.Equal("Production", Assert.IsType<StsEnvironmentDataModel>(record.Contract).Name);
    }

    [Fact]
    public async Task GetServerRecords_WhenServerIsUnavailable_ReturnsRequestFailed()
    {
        // Arrange
        _context.Server.IsUnavailable = true;

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
    }

    [Fact]
    public async Task Upsert_WhenRecordIsNew_CreatesItWithExpectedVersionZero()
    {
        // Arrange
        var contract = new StsEnvironmentDataModel { Name = "Production", Description = "Live" };

        // Act
        Result<int> result = await _sut.Upsert("Production", contract, 0, default);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.Equal("Live", _context.Server.Get<StsEnvironmentDataModel>(Area, "Production")?.Description);
        Assert.Equal(["POST /api/v1/environments/update/Production"], _context.Server.Requests);
    }

    [Fact]
    public async Task Upsert_WhenVersionIsExpected_UpdatesAndReturnsTheNewVersion()
    {
        // Arrange
        _context.Server.Store(Area, "Production", new StsEnvironmentDataModel { Name = "Production" }, 3);
        var contract = new StsEnvironmentDataModel { Name = "Production", Description = "Live" };

        // Act
        Result<int> result = await _sut.Upsert("Production", contract, 3, default);

        // Assert
        Assert.Equal(4, result.Value);
        Assert.Equal("Live", _context.Server.Get<StsEnvironmentDataModel>(Area, "Production")?.Description);
    }

    //the engine recognizes the server's 409 by its code
    [Fact]
    public async Task Upsert_WhenServerHasAnotherVersion_ReturnsConcurrencyConflict()
    {
        // Arrange
        _context.Server.Store(Area, "Production", new StsEnvironmentDataModel { Name = "Production" }, 4);

        // Act
        Result<int> result = await _sut.Upsert("Production", new StsEnvironmentDataModel { Name = "Production" }, 3,
            default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
    }

    [Fact]
    public async Task Delete_WhenVersionIsExpected_DeletesWithTheVersion()
    {
        // Arrange
        _context.Server.Store(Area, "Production", new StsEnvironmentDataModel { Name = "Production" }, 2);

        // Act
        Result result = await _sut.Delete("Production", 2, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Server.Records(Area));
        Assert.Equal(["DELETE /api/v1/environments/delete/Production?version=2"], _context.Server.Requests);
    }

    [Fact]
    public async Task Delete_WhenRecordIsMissing_ReturnsRecordWithNameNotFound()
    {
        // Act
        Result result = await _sut.Delete("Production", 2, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RecordWithNameNotFound, result.Error.Code);
    }

    [Fact]
    public void ApplyLocal_WhenRecordExists_ReplacesTheValueUnderTheLocalKey()
    {
        // Arrange
        Dictionary<string, string> environments = _context.Parameters.Environments;
        environments["Production"] = "Old";

        // Act
        _sut.ApplyLocal("Production", new StsEnvironmentDataModel { Name = "PRODUCTION", Description = "New" });

        // Assert
        Assert.Same(environments, _context.Parameters.Environments);
        Assert.Equal(["Production"], environments.Keys);
        Assert.Equal("New", environments["Production"]);
    }

    [Fact]
    public void ApplyLocal_WhenRecordIsNew_AddsIt()
    {
        // Act
        _sut.ApplyLocal("Production", new StsEnvironmentDataModel { Name = "Production" });

        // Assert
        Assert.Equal(string.Empty, _context.Parameters.Environments["Production"]);
    }

    [Fact]
    public void RemoveLocal_WhenCalled_RemovesTheRecord()
    {
        // Arrange
        _context.Parameters.Environments["Production"] = "Live";
        _context.Parameters.Environments["Development"] = "Dev";

        // Act
        _sut.RemoveLocal("Production");

        // Assert
        Assert.Equal(["Development"], _context.Parameters.Environments.Keys);
    }

    [Fact]
    public void Normalize_WhenDescriptionIsEmptyOrMissing_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsEnvironmentDataModel { Name = "Production", Description = "" };
        var missing = new StsEnvironmentDataModel { Name = "Production", Version = 5 };

        // Act
        string emptyHash = RegistryContractHasher.ComputeHash(_sut.Normalize(empty));
        string missingHash = RegistryContractHasher.ComputeHash(_sut.Normalize(missing));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }
}
