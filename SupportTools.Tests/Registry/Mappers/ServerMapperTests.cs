using LibSupportToolsServerWork.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class ServerMapperTests
{
    //IsLocal belongs to the computer: the local record keeps it
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApplyToLocal_WhenContractComesFromLocalRecord_RestoresTheOriginal(bool isLocal)
    {
        // Arrange
        ServerDataModel original = NewServer(isLocal);
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

    private static ServerDataModel NewServer(bool isLocal)
    {
        return new ServerDataModel
        {
            IsLocal = isLocal,
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
