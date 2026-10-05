using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Mappers;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Mappers;

public sealed class GitMapperTests
{
    private const string CanonicalFolderName = @"Front\{SpaProjectFolderRelativePath}\app";
    private const string LinuxFolderName = "Front/{SpaProjectFolderRelativePath}/app";

    [Fact]
    public void ApplyToLocal_WhenContractComesFromLocalRecordOnWindows_RestoresTheOriginal()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.WindowsPathMapper();
        GitDataModel original = NewGit(CanonicalFolderName);
        StsGitDataModel contract = GitMapper.ToContract("AppFront", original, pathMapper);
        var restored = new GitDataModel();

        // Act
        GitMapper.ApplyToLocal(contract, restored, pathMapper);

        // Assert
        Assert.Equal("AppFront", contract.GitProjectName);
        Assert.Equal(MapperTestHelpers.JsonOf(original), MapperTestHelpers.JsonOf(restored));
    }

    //the folder name is relative: no prefix mapping, Linux only swaps the separators
    [Fact]
    public void ToContract_WhenComputerIsLinux_SendsCanonicalSeparators()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();

        // Act
        StsGitDataModel result = GitMapper.ToContract("AppFront", NewGit(LinuxFolderName), pathMapper);

        // Assert
        Assert.Equal(CanonicalFolderName, result.GitProjectFolderName);
        Assert.Empty(pathMapper.Issues);
    }

    [Fact]
    public void ApplyToLocal_WhenComputerIsLinux_StoresLinuxSeparators()
    {
        // Arrange
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        StsGitDataModel contract =
            GitMapper.ToContract("AppFront", NewGit(CanonicalFolderName), MapperTestHelpers.WindowsPathMapper());
        var local = new GitDataModel();

        // Act
        GitMapper.ApplyToLocal(contract, local, pathMapper);

        // Assert
        Assert.Equal(LinuxFolderName, local.GitProjectFolderName);
    }

    //the contract fields are required: a missing local value goes as an empty text, which the server refuses
    [Fact]
    public void ToContract_WhenLocalFieldsAreMissing_SendsEmptyTexts()
    {
        // Act
        StsGitDataModel result =
            GitMapper.ToContract("AppFront", new GitDataModel(), MapperTestHelpers.WindowsPathMapper());

        // Assert
        Assert.Equal(string.Empty, result.GitProjectAddress);
        Assert.Equal(string.Empty, result.GitProjectFolderName);
        Assert.Equal(string.Empty, result.GitIgnorePatternName);
    }

    private static GitDataModel NewGit(string folderName)
    {
        return new GitDataModel
        {
            GitProjectAddress = "git@github.com:fake/AppFront.git",
            GitProjectFolderName = folderName,
            GitIgnorePatternName = "React"
        };
    }
}
