using System.Collections.Generic;
using DatabaseTools.DbTools.Models;
using LibSupportToolsServerWork.Registry.Mappers;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class DatabaseServerConnectionMapperTests
{
    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalRecord_RestoresTheOriginal()
    {
        // Arrange
        DatabaseServerConnectionData original = NewConnection();
        StsDatabaseServerConnectionDataModel contract = DatabaseServerConnectionMapper.ToContract("Dev", original);
        var restored = new DatabaseServerConnectionData();

        // Act
        DatabaseServerConnectionMapper.ApplyToLocal(contract, restored);

        // Assert
        Assert.Equal("SqlServer", contract.DatabaseServerProvider);
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    //the folders sets belong to the record as a whole, but the dictionary instance stays (editors keep it)
    [Fact]
    public void ApplyToLocal_WhenServerHasOtherFoldersSets_ReplacesThemInTheSameDictionary()
    {
        // Arrange
        DatabaseServerConnectionData local = NewConnection();
        Dictionary<string, DatabaseFoldersSet>? foldersSets = local.DatabaseFoldersSets;
        var contract = new StsDatabaseServerConnectionDataModel
        {
            Name = "Dev",
            DatabaseServerProvider = "SqlServer",
            DatabaseFoldersSets = [new StsDatabaseFoldersSetDataModel { Name = "Fast", Data = @"F:\Data" }]
        };

        // Act
        DatabaseServerConnectionMapper.ApplyToLocal(contract, local);

        // Assert
        Assert.Same(foldersSets, local.DatabaseFoldersSets);
        Assert.Equal(["Fast"], local.DatabaseFoldersSets!.Keys);
        Assert.Equal(@"F:\Data", local.DatabaseFoldersSets["Fast"].Data);
    }

    [Fact]
    public void ToContract_WhenLocalHasNoFoldersSets_SendsAnEmptyList()
    {
        // Act
        StsDatabaseServerConnectionDataModel result = DatabaseServerConnectionMapper.ToContract("Dev",
            new DatabaseServerConnectionData { DatabaseFoldersSets = null });

        // Assert
        Assert.Empty(result.DatabaseFoldersSets);
    }

    //the name may come from another client in another case
    [Fact]
    public void ApplyToLocal_WhenProviderNameDiffersInCase_StoresTheProvider()
    {
        // Arrange
        var local = new DatabaseServerConnectionData();

        // Act
        DatabaseServerConnectionMapper.ApplyToLocal(
            new StsDatabaseServerConnectionDataModel { Name = "Dev", DatabaseServerProvider = "sqlserver" }, local);

        // Assert
        Assert.Equal(EDatabaseProvider.SqlServer, local.DatabaseServerProvider);
    }

    [Fact]
    public void ApplyToLocal_WhenLocalHasNoFoldersSets_CreatesThem()
    {
        // Arrange
        var local = new DatabaseServerConnectionData { DatabaseFoldersSets = null };
        StsDatabaseServerConnectionDataModel contract = DatabaseServerConnectionMapper.ToContract("Dev",
            NewConnection());

        // Act
        DatabaseServerConnectionMapper.ApplyToLocal(contract, local);

        // Assert
        Assert.Equal(["Default", "Archive"], local.DatabaseFoldersSets!.Keys);
    }

    //the server returns the folders sets ordered by name and keeps no empty text; the provider name may differ in case
    [Fact]
    public void Normalize_WhenOrderEmptyTextsOrProviderCaseDiffer_GivesTheSameHash()
    {
        // Arrange
        StsDatabaseServerConnectionDataModel local = DatabaseServerConnectionMapper.ToContract("Dev", NewConnection());
        local.DbWebAgentName = "";
        var server = new StsDatabaseServerConnectionDataModel
        {
            Name = "Dev",
            DatabaseServerProvider = "sqlserver",
            RemoteDbConnectionName = "RemoteDev",
            ServerAddress = @"(localdb)\Dev",
            WindowsNtIntegratedSecurity = true,
            ServerUser = "fake-user",
            ServerPass = "fake-password",
            TrustServerCertificate = true,
            ConnectionTimeOut = 30,
            Encrypt = true,
            DatabaseFoldersSets =
            [
                new StsDatabaseFoldersSetDataModel
                {
                    Name = "Archive", Backup = @"E:\Archive", Data = "", DataLog = null
                },
                new StsDatabaseFoldersSetDataModel
                {
                    Name = "Default", Backup = @"D:\Backup", Data = @"D:\Data", DataLog = @"D:\Log"
                }
            ],
            Version = 7
        };

        // Act
        string localHash = MapperTestHelpers.HashOf(DatabaseServerConnectionMapper.Normalize(local));
        string serverHash = MapperTestHelpers.HashOf(DatabaseServerConnectionMapper.Normalize(server));

        // Assert
        Assert.Equal(localHash, serverHash);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmptyOrNull_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsDatabaseServerConnectionDataModel
        {
            Name = "Dev",
            DatabaseServerProvider = "SqlServer",
            DbWebAgentName = "",
            RemoteDbConnectionName = "",
            ServerAddress = "",
            ServerUser = "",
            ServerPass = "",
            DatabaseFoldersSets =
            [
                new StsDatabaseFoldersSetDataModel { Name = "Default", Backup = "", Data = "", DataLog = "" }
            ]
        };
        var missing = new StsDatabaseServerConnectionDataModel
        {
            Name = "Dev",
            DatabaseServerProvider = "SqlServer",
            DatabaseFoldersSets = [new StsDatabaseFoldersSetDataModel { Name = "Default" }]
        };

        // Act
        string emptyHash = MapperTestHelpers.HashOf(DatabaseServerConnectionMapper.Normalize(empty));
        string missingHash = MapperTestHelpers.HashOf(DatabaseServerConnectionMapper.Normalize(missing));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }

    [Theory]
    [InlineData("SqlServer", false)]
    [InlineData("Oracle", true)]
    public void HasUnknownProvider_WhenCalled_FindsProvidersUnknownToThisClient(string provider, bool expected)
    {
        // Arrange
        var contract = new StsDatabaseServerConnectionDataModel { Name = "Dev", DatabaseServerProvider = provider };

        // Act
        bool result = DatabaseServerConnectionMapper.HasUnknownProvider(contract);

        // Assert
        Assert.Equal(expected, result);
    }

    //every field has a value other than its default, so the round trip shows a field that is not copied
    private static DatabaseServerConnectionData NewConnection()
    {
        return new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.SqlServer,
            DbWebAgentName = "Pc1.WebAgent",
            RemoteDbConnectionName = "RemoteDev",
            Encrypt = true,
            ServerAddress = @"(localdb)\Dev",
            WindowsNtIntegratedSecurity = true,
            ServerUser = "fake-user",
            ServerPass = "fake-password",
            TrustServerCertificate = true,
            ConnectionTimeOut = 30,
            DatabaseFoldersSets = new Dictionary<string, DatabaseFoldersSet>
            {
                ["Default"] = new() { Backup = @"D:\Backup", Data = @"D:\Data", DataLog = @"D:\Log" },
                ["Archive"] = new() { Backup = @"E:\Archive" }
            }
        };
    }
}
