using System;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ServerMapperTests
{
    //IsLocal belongs to the computer: the local record keeps it. IsDecommissioned travels both ways
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ApplyToLocal_WhenContractComesFromLocalRecord_RestoresTheOriginal(bool isLocal, bool isDecommissioned)
    {
        // Arrange
        ServerDataModel original = NewServer(isLocal, isDecommissioned);
        StsServerDataModel contract = ServerMapper.ToContract("dl360", original);
        var restored = new ServerDataModel { IsLocal = isLocal };

        // Act
        ServerMapper.ApplyToLocal(contract, restored);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    [Fact]
    public void ToContract_WhenCalled_CopiesTargetServerPathsUnchanged()
    {
        // Act
        StsServerDataModel result = ServerMapper.ToContract("dl360", NewServer(true));

        // Assert
        Assert.Equal("dl360", result.Name);
        Assert.Equal("/srv/download", result.ServerSideDownloadFolder);
        Assert.Equal(@"C:\Apps", result.ServerSideDeployFolder);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmptyOrNull_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsServerDataModel
        {
            Name = "S",
            WebAgentName = "",
            WebAgentInstallerName = "",
            FilesUserName = "",
            FilesUsersGroupName = "",
            Runtime = "",
            ServerSideDownloadFolder = "",
            ServerSideDeployFolder = ""
        };
        var missing = new StsServerDataModel { Name = "S" };

        // Act
        string emptyHash = MapperTestHelpers.HashOf(ServerMapper.Normalize(empty));
        string missingHash = MapperTestHelpers.HashOf(ServerMapper.Normalize(missing));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }

    //false is a default value, which the hash leaves out: a server in use keeps the hash it had before the field existed
    [Fact]
    public void IsDecommissioned_WhenFalse_IsLeftOutOfTheHash()
    {
        // Arrange
        StsServerDataModel inUse = ServerMapper.ToContract("dl360", NewServer(true));
        StsServerDataModel decommissioned = ServerMapper.ToContract("dl360", NewServer(true, true));

        // Act
        string inUseJson = RegistryContractHasher.ToCanonicalJson(inUse);
        string decommissionedJson = RegistryContractHasher.ToCanonicalJson(decommissioned);

        // Assert
        Assert.DoesNotContain(nameof(StsServerDataModel.IsDecommissioned), inUseJson, StringComparison.Ordinal);
        Assert.Contains("\"IsDecommissioned\":true", decommissionedJson, StringComparison.Ordinal);
    }

    private static ServerDataModel NewServer(bool isLocal, bool isDecommissioned = false)
    {
        return new ServerDataModel
        {
            IsLocal = isLocal,
            IsDecommissioned = isDecommissioned,
            WebAgentName = "Dl360.WebAgent",
            WebAgentInstallerName = "Dl360.Installer",
            FilesUserName = "appuser",
            FilesUsersGroupName = "appgroup",
            Runtime = "linux-x64",
            ServerSideDownloadFolder = "/srv/download",
            ServerSideDeployFolder = @"C:\Apps"
        };
    }
}
