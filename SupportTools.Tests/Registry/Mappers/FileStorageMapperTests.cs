using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using ParametersManagement.LibFileParameters.Models;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class FileStorageMapperTests
{
    private const string CanonicalStoragePath = @"D:\1WorkDotnet\Backups\Exchange";
    private const string LinuxStoragePath = "/home/u/1WorkDotnet/Backups/Exchange";
    private const string FtpStoragePath = "ftp://files.example.test/backups";

    [Theory]
    [InlineData(CanonicalStoragePath)]
    [InlineData(FtpStoragePath)]
    [InlineData(null)]
    public void ApplyToLocal_WhenContractComesFromLocalRecordOnWindows_RestoresTheOriginal(string? storagePath)
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.WindowsPathMapper();
        FileStorageData original = NewFileStorage(storagePath);
        StsFileStorageDataModel contract = FileStorageMapper.ToContract("Exchange", original, pathMapper);
        var restored = new FileStorageData();

        // Act
        FileStorageMapper.ApplyToLocal(contract, restored, pathMapper);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    //a Linux computer sends the canonical (Windows) form and gets its own form back
    [Fact]
    public void ToContract_WhenLocalPathIsOnLinux_SendsCanonicalPath()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();

        // Act
        StsFileStorageDataModel result =
            FileStorageMapper.ToContract("Exchange", NewFileStorage(LinuxStoragePath), pathMapper);

        // Assert
        Assert.Equal(CanonicalStoragePath, result.FileStoragePath);
        Assert.Empty(pathMapper.Issues);
    }

    [Fact]
    public void ApplyToLocal_WhenComputerIsLinux_StoresLocalPath()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        StsFileStorageDataModel contract = FileStorageMapper.ToContract("Exchange",
            NewFileStorage(CanonicalStoragePath), MapperTestHelpers.WindowsPathMapper());
        var local = new FileStorageData();

        // Act
        FileStorageMapper.ApplyToLocal(contract, local, pathMapper);

        // Assert
        Assert.Equal(LinuxStoragePath, local.FileStoragePath);
        Assert.Empty(pathMapper.Issues);
    }

    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalRecordOnLinux_RestoresTheOriginal()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        FileStorageData original = NewFileStorage(LinuxStoragePath);
        StsFileStorageDataModel contract = FileStorageMapper.ToContract("Exchange", original, pathMapper);
        var restored = new FileStorageData();

        // Act
        FileStorageMapper.ApplyToLocal(contract, restored, pathMapper);

        // Assert
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    //a URL is no path: no rule maps it and it is no issue
    [Fact]
    public void ToContract_WhenPathIsUrlOnLinux_SendsItUnchangedWithoutIssue()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();

        // Act
        StsFileStorageDataModel result =
            FileStorageMapper.ToContract("Exchange", NewFileStorage(FtpStoragePath), pathMapper);

        // Assert
        Assert.Equal(FtpStoragePath, result.FileStoragePath);
        Assert.Empty(pathMapper.Issues);
    }

    //a URL is never mapped, even when the beginning of a rule matches it
    [Fact]
    public void ApplyToLocal_WhenRuleMatchesTheBeginningOfUrl_StoresTheUrlUnchanged()
    {
        // Arrange
        List<PathMappingModel> pathMappings =
        [
            new() { CanonicalPrefix = "ftp://files.example.test", LocalPrefix = "/mnt/files" }
        ];
        var pathMapper = new PathMapper(pathMappings, '/');
        var contract = new StsFileStorageDataModel { Name = "Exchange", FileStoragePath = FtpStoragePath };
        var local = new FileStorageData();

        // Act
        FileStorageMapper.ApplyToLocal(contract, local, pathMapper);

        // Assert
        Assert.Equal(FtpStoragePath, local.FileStoragePath);
    }

    [Fact]
    public void ApplyToLocal_WhenPathIsUrlOnLinux_StoresItUnchanged()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        var contract = new StsFileStorageDataModel { Name = "Exchange", FileStoragePath = FtpStoragePath };
        var local = new FileStorageData();

        // Act
        FileStorageMapper.ApplyToLocal(contract, local, pathMapper);

        // Assert
        Assert.Equal(FtpStoragePath, local.FileStoragePath);
        Assert.Empty(pathMapper.Issues);
    }

    //the main computer keeps some paths with a lowercase drive letter; a Linux computer gives them back with the
    //spelling of its rule (D:\), and both must hash alike
    [Fact]
    public void Normalize_WhenLinuxRoundTripsALowercaseDriveLetter_GivesTheSameHash()
    {
        // Arrange
        StsFileStorageDataModel main = FileStorageMapper.ToContract("Exchange",
            NewFileStorage(@"d:\1WorkDotnet\Backups\Exchange"), MapperTestHelpers.WindowsPathMapper());
        PathMapper linuxPathMapper = MapperTestHelpers.LinuxPathMapper();
        var linuxLocal = new FileStorageData();
        FileStorageMapper.ApplyToLocal(main, linuxLocal, linuxPathMapper);
        StsFileStorageDataModel linux = FileStorageMapper.ToContract("Exchange", linuxLocal, linuxPathMapper);

        // Act
        string mainHash = MapperTestHelpers.HashOf(FileStorageMapper.Normalize(main));
        string linuxHash = MapperTestHelpers.HashOf(FileStorageMapper.Normalize(linux));

        // Assert
        Assert.Equal(LinuxStoragePath, linuxLocal.FileStoragePath);
        Assert.Equal(mainHash, linuxHash);
    }

    [Fact]
    public void Normalize_WhenTextsAreEmptyOrNull_GivesTheSameHash()
    {
        // Arrange
        var empty = new StsFileStorageDataModel { Name = "E", FileStoragePath = "", UserName = "", Password = "" };
        var missing = new StsFileStorageDataModel { Name = "E" };

        // Act
        string emptyHash = MapperTestHelpers.HashOf(FileStorageMapper.Normalize(empty));
        string missingHash = MapperTestHelpers.HashOf(FileStorageMapper.Normalize(missing));

        // Assert
        Assert.Equal(missingHash, emptyHash);
    }

    private static FileStorageData NewFileStorage(string? storagePath)
    {
        return new FileStorageData
        {
            FileStoragePath = storagePath,
            UserName = "fake-user",
            Password = "fake-password",
            //FileNameMaxLength = 255,
            //FileSizeSplitPositionInRow = 4,
            FtpSiteLsFileOffset = 8
        };
    }
}
