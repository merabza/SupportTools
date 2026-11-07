using System;
using System.IO;
using System.Linq;
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

    //an empty file has the same bytes as no file, yet it was created
    [Fact]
    public void HasChanged_WhenAnEmptyFileWasCreated_ReturnsTrue()
    {
        // Arrange
        File.WriteAllBytes(_filePath, []);

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

    [Fact]
    public void CountChangedVersions_WhenVersionsChanged_CountsOnlyThem()
    {
        // Arrange
        byte[] bytesBefore = WithBom(Versions(("A", "1.0.0"), ("B", "2.0.0"), ("C", "3.0.0")));
        byte[] bytesAfter = Encoding.UTF8.GetBytes(Versions(("A", "1.0.1"), ("B", "2.0.0"), ("C", "3.1.0")));

        // Act
        int result = CentralPackageVersionsFile.CountChangedVersions(bytesBefore, bytesAfter);

        // Assert
        Assert.Equal(2, result);
    }

    //NuGet compares package names without case
    [Fact]
    public void CountChangedVersions_WhenOnlyTheCaseOfANameDiffers_CountsNothing()
    {
        // Act
        int result = CentralPackageVersionsFile.CountChangedVersions(
            Encoding.UTF8.GetBytes(Versions(("Polly", "8.8.0"))), Encoding.UTF8.GetBytes(Versions(("polly", "8.8.0"))));

        // Assert
        Assert.Equal(0, result);
    }

    //dotnet outdated -n writes these entries too
    [Fact]
    public void CountChangedVersions_WhenAGlobalPackageReferenceOrAnUpdateEntryChanged_CountsThem()
    {
        // Arrange
        const string textBefore =
            "<GlobalPackageReference Include=\"SonarAnalyzer.CSharp\" Version=\"10.34.0\" />\r\n" +
            "<PackageVersion Update=\"Polly\" Version=\"8.8.0\" />\r\n";
        string textAfter = textBefore.Replace("10.34.0", "10.35.0", StringComparison.Ordinal)
            .Replace("8.8.0", "8.9.0", StringComparison.Ordinal);

        // Act
        int result = CentralPackageVersionsFile.CountChangedVersions(Encoding.UTF8.GetBytes(textBefore),
            Encoding.UTF8.GetBytes(textAfter));

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void CountChangedVersions_WhenTheFileDidNotExistBefore_CountsEveryVersion()
    {
        // Act
        int result = CentralPackageVersionsFile.CountChangedVersions(null,
            Encoding.UTF8.GetBytes(Versions(("A", "1.0.0"), ("B", "2.0.0"))));

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void CountChangedVersions_WhenTheFileIsGone_CountsNothing()
    {
        // Act
        int result = CentralPackageVersionsFile.CountChangedVersions(WithBom(Versions(("A", "1.0.0"))), null);

        // Assert
        Assert.Equal(0, result);
    }

    private static string Versions(params (string Name, string Version)[] packages)
    {
        return string.Concat(packages.Select(x =>
            $"    <PackageVersion Include=\"{x.Name}\" Version=\"{x.Version}\" />\r\n"));
    }

    private static byte[] WithBom(string text)
    {
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)];
    }
}
