using System;
using System.Collections.Generic;
using SupportTools.Menu.SyncRegistry;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Menu.SyncRegistry;

//all secret values are made up
public sealed class RegistryRecordDiffTests
{
    //the root Version is the optimistic concurrency of the server, not content
    [Fact]
    public void Compare_WhenFieldsDiffer_ReturnsTheirPathsAndValuesWithoutTheVersion()
    {
        // Arrange
        var local = new StsEnvironmentDataModel { Name = "Production", Description = "Live" };
        var server = new StsEnvironmentDataModel { Name = "Production", Description = "Old", Version = 5 };

        // Act
        List<RegistryFieldDifference> result = RegistryRecordDiff.Compare(local, server);

        // Assert
        Assert.Equal([new RegistryFieldDifference("Description", "Live", "Old")], result);
    }

    [Fact]
    public void Compare_WhenContractsAreEqual_ReturnsNothing()
    {
        // Arrange
        var local = new StsApiClientDataModel { Name = "Pc1", Server = "http://pc1.example.test", ApiKey = "fake-1" };
        var server = new StsApiClientDataModel
        {
            Name = "Pc1", Server = "http://pc1.example.test", ApiKey = "fake-1", Version = 2
        };

        // Act
        List<RegistryFieldDifference> result = RegistryRecordDiff.Compare(local, server);

        // Assert
        Assert.Empty(result);
    }

    //the difference is found by the real values, but only the hidden marker leaves the diff
    [Theory]
    [InlineData("Password")]
    [InlineData("UserName")]
    [InlineData("ServerPass")]
    [InlineData("ServerUser")]
    [InlineData("ApiKey")]
    [InlineData("KeyGuidPart")]
    [InlineData("MediatRLicenseKey")]
    [InlineData("Content")]
    public void Compare_WhenSecretFieldDiffers_HidesBothValues(string fieldName)
    {
        // Arrange
        var local = new Dictionary<string, string> { ["Name"] = "Fake", [fieldName] = "fake-local-secret" };
        var server = new Dictionary<string, string> { ["Name"] = "Fake", [fieldName] = "fake-server-secret" };

        // Act
        List<RegistryFieldDifference> result = RegistryRecordDiff.Compare(local, server);

        // Assert
        Assert.Equal([new RegistryFieldDifference(fieldName, "***", "***")], result);
    }

    //a secret is hidden at any depth, and a missing side shows null
    [Fact]
    public void Compare_WhenNestedSecretExistsOnOneSideOnly_HidesItAndShowsTheMissingSideAsNull()
    {
        // Arrange
        var local = new { Name = "Dev", Connection = new { ServerAddress = "(localdb)\\Dev", ServerPass = "fake-1" } };
        var server = new { Name = "Dev", Connection = new { ServerAddress = "(localdb)\\Dev" } };

        // Act
        List<RegistryFieldDifference> result = RegistryRecordDiff.Compare(local, server);

        // Assert
        Assert.Equal([new RegistryFieldDifference("Connection.ServerPass", "***", null)], result);
    }

    //for example a record deleted on the server: every field of the other side differs
    [Fact]
    public void Compare_WhenOneSideIsMissing_ReturnsEveryFieldOfTheOtherSide()
    {
        // Arrange
        var local = new StsFileStorageDataModel
        {
            Name = "Exchange", FileStoragePath = @"D:\1WorkDotnet\Backups", Password = "fake-password"
        };

        // Act
        List<RegistryFieldDifference> result = RegistryRecordDiff.Compare(local, null);

        // Assert
        Assert.Equal([
            new RegistryFieldDifference("FileStoragePath", @"D:\1WorkDotnet\Backups", null),
            new RegistryFieldDifference("Name", "Exchange", null),
            new RegistryFieldDifference("Password", "***", null)
        ], result);
    }

    //list elements have indexed paths; the fields of the other side follow the local ones
    [Fact]
    public void Compare_WhenListsDiffer_ReturnsIndexedPaths()
    {
        // Arrange
        var local = new StsDatabaseServerConnectionDataModel
        {
            Name = "Dev",
            DatabaseServerProvider = "SqlServer",
            DatabaseFoldersSets = [new StsDatabaseFoldersSetDataModel { Name = "Default", Backup = @"D:\Backup" }]
        };
        var server = new StsDatabaseServerConnectionDataModel
        {
            Name = "Dev",
            DatabaseServerProvider = "SqlServer",
            DatabaseFoldersSets =
            [
                new StsDatabaseFoldersSetDataModel { Name = "Default", Backup = @"E:\Backup" },
                new StsDatabaseFoldersSetDataModel { Name = "Archive" }
            ]
        };

        // Act
        List<RegistryFieldDifference> result = RegistryRecordDiff.Compare(local, server);

        // Assert
        Assert.Equal([
            new RegistryFieldDifference("DatabaseFoldersSets[0].Backup", @"D:\Backup", @"E:\Backup"),
            new RegistryFieldDifference("DatabaseFoldersSets[1].Name", null, "Archive")
        ], result);
    }

    //numbers and booleans keep their JSON text; an empty list or object is a value of its own
    [Fact]
    public void Compare_WhenNonStringValuesDiffer_ShowsTheirJsonText()
    {
        // Arrange
        var local = new
        {
            Name = "Pc1", Port = 5, Enabled = true, Folders = Array.Empty<string>(), Options = new object()
        };
        var server = new { Name = "Pc1", Port = 6 };

        // Act
        List<RegistryFieldDifference> result = RegistryRecordDiff.Compare(local, server);

        // Assert
        Assert.Equal([
            new RegistryFieldDifference("Enabled", "true", null),
            new RegistryFieldDifference("Folders", "[]", null),
            new RegistryFieldDifference("Options", "{}", null),
            new RegistryFieldDifference("Port", "5", "6")
        ], result);
    }

    [Fact]
    public void GetChangedFieldNames_WhenNestedFieldsDiffer_ReturnsEveryTopLevelNameOnce()
    {
        // Arrange
        var local = new { Name = "AppX", SolutionFileName = @"D:\A.slnx", ServerInfos = new[] { "a", "b" } };
        var server = new { Name = "AppX", SolutionFileName = @"D:\B.slnx", ServerInfos = new[] { "c", "d" } };

        // Act
        List<string> result = RegistryRecordDiff.GetChangedFieldNames(local, server);

        // Assert
        Assert.Equal(["ServerInfos", "SolutionFileName"], result);
    }
}
