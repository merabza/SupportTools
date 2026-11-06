using System;
using System.IO;
using System.Text;
using LibGitWork.OutdatedPackages;
using Xunit;

namespace SupportTools.Tests.OutdatedPackages;

//dotnet outdated -n writes Directory.Packages.props with File.WriteAllText, which keeps the line breaks of the text and
//drops the UTF-8 BOM
public sealed class CentralPackageVersionsFileTests : IDisposable
{
    private const string OldText =
        "<Project>\r\n  <ItemGroup>\r\n    <PackageVersion Include=\"Polly\" Version=\"8.8.0\" />\r\n  </ItemGroup>\r\n</Project>\r\n";

    private const string NewText =
        "<Project>\r\n  <ItemGroup>\r\n    <PackageVersion Include=\"Polly\" Version=\"8.9.0\" />\r\n  </ItemGroup>\r\n</Project>\r\n";

    private readonly string _filePath;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public CentralPackageVersionsFileTests()
    {
        _filePath = Path.Combine(_tempFolder, CentralPackageVersionsFile.FileName);
    }

    public void Dispose()
    {
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void ReadBytes_WhenTheFileIsMissing_ReturnsNull()
    {
        // Act
        byte[]? result = CentralPackageVersionsFile.ReadBytes(_filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ReadBytes_WhenTheFileExists_ReturnsItsBytesWithTheBom()
    {
        // Arrange
        File.WriteAllBytes(_filePath, WithBom(OldText));

        // Act
        byte[]? result = CentralPackageVersionsFile.ReadBytes(_filePath);

        // Assert
        Assert.Equal(WithBom(OldText), result);
    }

    //the rest of the file, CRLF included, stays as dotnet outdated wrote it
    [Fact]
    public void RestoreLostBom_WhenTheFileLostItsBom_PutsItBack()
    {
        // Arrange
        File.WriteAllText(_filePath, NewText);

        // Act
        CentralPackageVersionsFile.RestoreLostBom(_filePath, WithBom(OldText));

        // Assert
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_filePath));
    }

    [Fact]
    public void RestoreLostBom_WhenTheFileHadNoBom_LeavesItWithoutOne()
    {
        // Arrange
        File.WriteAllText(_filePath, NewText);

        // Act
        CentralPackageVersionsFile.RestoreLostBom(_filePath, Encoding.UTF8.GetBytes(OldText));

        // Assert
        Assert.Equal(Encoding.UTF8.GetBytes(NewText), File.ReadAllBytes(_filePath));
    }

    [Fact]
    public void RestoreLostBom_WhenTheFileKeptItsBom_DoesNotAddAnotherOne()
    {
        // Arrange
        File.WriteAllBytes(_filePath, WithBom(NewText));

        // Act
        CentralPackageVersionsFile.RestoreLostBom(_filePath, WithBom(OldText));

        // Assert
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_filePath));
    }

    [Fact]
    public void RestoreLostBom_WhenTheFileDidNotExistBefore_LeavesItWithoutBom()
    {
        // Arrange
        File.WriteAllText(_filePath, NewText);

        // Act
        CentralPackageVersionsFile.RestoreLostBom(_filePath, null);

        // Assert
        Assert.Equal(Encoding.UTF8.GetBytes(NewText), File.ReadAllBytes(_filePath));
    }

    [Fact]
    public void RestoreLostBom_WhenTheFileIsGone_DoesNotCreateIt()
    {
        // Act
        CentralPackageVersionsFile.RestoreLostBom(_filePath, WithBom(OldText));

        // Assert
        Assert.False(File.Exists(_filePath));
    }

    [Fact]
    public void HasChanged_WhenTheFileIsTheSame_ReturnsFalse()
    {
        // Arrange
        File.WriteAllBytes(_filePath, WithBom(OldText));

        // Act
        bool result = CentralPackageVersionsFile.HasChanged(_filePath, WithBom(OldText));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasChanged_WhenAVersionChanged_ReturnsTrue()
    {
        // Arrange
        File.WriteAllBytes(_filePath, WithBom(NewText));

        // Act
        bool result = CentralPackageVersionsFile.HasChanged(_filePath, WithBom(OldText));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasChanged_WhenTheFileIsStillMissing_ReturnsFalse()
    {
        // Act
        bool result = CentralPackageVersionsFile.HasChanged(_filePath, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasChanged_WhenTheFileWasCreated_ReturnsTrue()
    {
        // Arrange
        File.WriteAllText(_filePath, NewText);

        // Act
        bool result = CentralPackageVersionsFile.HasChanged(_filePath, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasChanged_WhenTheFileWasDeleted_ReturnsTrue()
    {
        // Act
        bool result = CentralPackageVersionsFile.HasChanged(_filePath, WithBom(OldText));

        // Assert
        Assert.True(result);
    }

    private static byte[] WithBom(string text)
    {
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)];
    }
}
