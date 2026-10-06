using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DatabaseTools.DbTools.Models;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class DatabaseServerConnectionsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = "databaseserverconnections";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly DatabaseServerConnectionsRegistrySyncAdapter _sut;

    public DatabaseServerConnectionsRegistrySyncAdapterTests()
    {
        _context.Parameters.DatabaseServerConnections["Dev"] = new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.SqlServer,
            ServerAddress = @"(localdb)\Dev",
            ServerPass = "fake-password",
            DatabaseFoldersSets = new Dictionary<string, DatabaseFoldersSet>
            {
                ["Default"] = new() { Backup = @"D:\Backup" }
            }
        };
        _sut = new DatabaseServerConnectionsRegistrySyncAdapter(_context.ApiClient, _context.Parameters,
            _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetServerRecords_WhenProviderIsUnknown_LeavesTheRecordOutOnBothSidesWithWarning()
    {
        // Arrange
        _context.Server.Store(Area, "Dev",
            new StsDatabaseServerConnectionDataModel { Name = "Dev", DatabaseServerProvider = "Oracle" }, 2);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await _sut.GetServerRecords(default);
        IReadOnlyDictionary<string, object> localRecords = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Empty(localRecords);
        Assert.Equal("Dev", Assert.Single(_context.Warnings.Items).Key);
    }

    [Fact]
    public void ApplyLocal_WhenRecordExists_UpdatesTheSameObjectAndFoldersSetsDictionary()
    {
        // Arrange
        DatabaseServerConnectionData existing = _context.Parameters.DatabaseServerConnections["Dev"];
        Dictionary<string, DatabaseFoldersSet>? foldersSets = existing.DatabaseFoldersSets;

        // Act
        _sut.ApplyLocal("Dev",
            new StsDatabaseServerConnectionDataModel
            {
                Name = "Dev",
                DatabaseServerProvider = "SqLite",
                DatabaseFoldersSets = [new StsDatabaseFoldersSetDataModel { Name = "Fast", Data = @"F:\Data" }]
            });

        // Assert
        Assert.Same(existing, _context.Parameters.DatabaseServerConnections["Dev"]);
        Assert.Same(foldersSets, existing.DatabaseFoldersSets);
        Assert.Equal(EDatabaseProvider.SqLite, existing.DatabaseServerProvider);
        Assert.Null(existing.ServerPass);
        Assert.Equal(["Fast"], foldersSets!.Keys);
    }
}
