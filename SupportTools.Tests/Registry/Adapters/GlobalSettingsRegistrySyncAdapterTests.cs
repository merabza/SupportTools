using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//a singleton (B5): one record with the key Global. An empty record counts as missing on both sides (the user's
//decision in C3), so a new computer pulls it without a FirstSyncDiffers conflict
[Collection(ConsoleCaptureCollection.Name)]
public sealed class GlobalSettingsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = FakeSupportToolsServer.GlobalSettings;
    private const string Key = GlobalSettingsRegistrySyncAdapter.RecordKey;
    private readonly RegistryAdapterTestContext _context = new();
    private readonly GlobalSettingsRegistrySyncAdapter _sut;

    public GlobalSettingsRegistrySyncAdapterTests()
    {
        _sut = new GlobalSettingsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues()
    {
        // Assert
        Assert.Equal((RegistryCollections.GlobalSettings, RegistryCollections.GlobalSettingsOrder),
            (_sut.CollectionName, _sut.Order));
    }

    [Fact]
    public async Task GetServerRecords_WhenServerIsUnavailable_ReturnsRequestFailed()
    {
        // Arrange
        _context.Server.IsUnavailable = true;

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
    }

    //an expected version other than 0 goes as it is, whatever the last read saw
    [Fact]
    public async Task Upsert_WhenVersionIsExpected_UpdatesWithIt()
    {
        // Arrange
        _context.Server.Store(Area, string.Empty, new StsGlobalSettingsDataModel { UploadTempExtension = ".up!" }, 3);

        // Act
        Result<int> result = await _sut.Upsert(Key,
            new StsGlobalSettingsDataModel { UploadTempExtension = ".new!" }, 3, default);

        // Assert
        Assert.Equal(4, result.Value);
        Assert.Equal(".new!",
            _context.Server.Get<StsGlobalSettingsDataModel>(Area, string.Empty)?.UploadTempExtension);
    }

    [Fact]
    public void GetLocalRecords_WhenSharedFieldsAreEmpty_GivesNoRecord()
    {
        // Arrange
        _context.Parameters.SupportToolsServerWebApiClientName = "SupportToolsServer";
        _context.Parameters.UploadTempExtension = "";
        _context.Parameters.DatabasesBackupFilesExchangeParameters =
            new DatabasesBackupFilesExchangeParameters { LocalPath = @"D:\Local" };

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetLocalRecords_WhenSharedFieldIsSet_GivesTheRecordWithTheSingletonKey()
    {
        // Arrange
        _context.Parameters.MediatRLicenseKey = "fake-license-key";

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal("Global", Key);
        Assert.Equal("fake-license-key",
            Assert.IsType<StsGlobalSettingsDataModel>(result[Key]).MediatRLicenseKey);
    }

    //before the first create the server answers an empty record with version 0
    [Fact]
    public async Task GetServerRecords_WhenServerHasNoRecord_GivesNoRecord()
    {
        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetServerRecords_WhenServerRecordHasData_GivesItWithItsVersion()
    {
        // Arrange
        _context.Server.Store(Area, string.Empty, new StsGlobalSettingsDataModel { UploadTempExtension = ".up!" }, 3);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(3, result.Value[Key].Version);
    }

    [Fact]
    public async Task Upsert_WhenServerHasNoRecord_CreatesItWithVersionZero()
    {
        // Arrange
        await _sut.GetServerRecords(default);

        // Act
        Result<int> result = await _sut.Upsert(Key,
            new StsGlobalSettingsDataModel { ServiceDescriptionSignature = "Fake" }, 0, default);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.Equal("Fake",
            _context.Server.Get<StsGlobalSettingsDataModel>(Area, string.Empty)?.ServiceDescriptionSignature);
    }

    //an emptied record looks missing but keeps its row and version: creating it again updates that version
    [Fact]
    public async Task Upsert_WhenServerRecordWasEmptied_RecreatesItOverItsVersion()
    {
        // Arrange
        _context.Server.Store(Area, string.Empty, new StsGlobalSettingsDataModel(), 4);
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords =
            await _sut.GetServerRecords(default);

        // Act
        Result<int> result = await _sut.Upsert(Key,
            new StsGlobalSettingsDataModel { ServiceDescriptionSignature = "Fake" }, 0, default);

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Equal(5, result.Value);
    }

    //the server deletes no singleton: deleting means writing an empty record
    [Fact]
    public async Task Delete_WhenVersionIsExpected_WritesAnEmptyRecord()
    {
        // Arrange
        _context.Server.Store(Area, string.Empty, new StsGlobalSettingsDataModel { UploadTempExtension = ".up!" }, 3);

        // Act
        Result result = await _sut.Delete(Key, 3, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, _context.Server.VersionOf(Area, string.Empty));
        Assert.Empty((await _sut.GetServerRecords(default)).Value);
    }

    [Fact]
    public async Task Delete_WhenServerHasAnotherVersion_ReturnsConcurrencyConflict()
    {
        // Arrange
        _context.Server.Store(Area, string.Empty, new StsGlobalSettingsDataModel { UploadTempExtension = ".up!" }, 4);

        // Act
        Result result = await _sut.Delete(Key, 3, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
    }

    [Fact]
    public void ApplyLocal_WhenCalled_SetsSharedFieldsAndKeepsMachineFields()
    {
        // Arrange
        _context.Parameters.LogFolder = @"D:\Logs";
        _context.Parameters.DatabasesBackupFilesExchangeParameters =
            new DatabasesBackupFilesExchangeParameters { LocalPath = @"D:\Local" };
        var contract = new StsGlobalSettingsDataModel
        {
            ProgramArchiveExtension = ".zip",
            DatabasesBackupFilesExchange = new StsDatabasesBackupFilesExchangeDataModel { DownloadTempExtension = ".d" }
        };

        // Act
        _sut.ApplyLocal(Key, contract);

        // Assert
        Assert.Equal(".zip", _context.Parameters.ProgramArchiveExtension);
        Assert.Equal(@"D:\Logs", _context.Parameters.LogFolder);
        Assert.Equal(".d", _context.Parameters.DatabasesBackupFilesExchangeParameters.DownloadTempExtension);
        Assert.Equal(@"D:\Local", _context.Parameters.DatabasesBackupFilesExchangeParameters.LocalPath);
    }

    //the record is emptied, so it disappears from the local records, as the engine checks after a removal
    [Fact]
    public void RemoveLocal_WhenCalled_ClearsSharedFieldsAndKeepsMachineFields()
    {
        // Arrange
        _context.Parameters.LogFolder = @"D:\Logs";
        _context.Parameters.MediatRLicenseKey = "fake-license-key";
        _context.Parameters.DatabasesBackupFilesExchangeParameters = new DatabasesBackupFilesExchangeParameters
        {
            LocalPath = @"D:\Local", ExchangeFileStorageName = "exchange"
        };

        // Act
        _sut.RemoveLocal(Key);

        // Assert
        Assert.Empty(_sut.GetLocalRecords());
        Assert.Null(_context.Parameters.MediatRLicenseKey);
        Assert.Null(_context.Parameters.DatabasesBackupFilesExchangeParameters.ExchangeFileStorageName);
        Assert.Equal(@"D:\Local", _context.Parameters.DatabasesBackupFilesExchangeParameters.LocalPath);
        Assert.Equal(@"D:\Logs", _context.Parameters.LogFolder);
    }
}
