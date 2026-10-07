using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//reading and writing a stored file (C6): only UTF-8 text up to the limit of B8 is synced, and the BOM is not part of
//the content. All contents are made up
public sealed class LocalStoredFileTests : IDisposable
{
    private const string Content = "{\"ConnectionString\":\"fake-connection-0001\",\"Name\":\"ფაილი\"}\r\n";
    private const string NotTextProblem = "is not a UTF-8 text file";
    private const string TooLargeProblem = "is larger than 1048576 bytes";

    private readonly string _folder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    [Fact]
    public void Read_WhenFileIsUtf8Text_GivesItsContentWithTheHashAndLengthOfB8()
    {
        // Arrange
        string path = WriteFile("appsettings.json", Content, false);

        // Act
        (LocalStoredFileContent? content, string? problem) = LocalStoredFile.Read(path);

        // Assert
        Assert.Null(problem);
        Assert.Equal(
            new LocalStoredFileContent(Content, FakeSupportToolsServer.Sha256Of(Content),
                Encoding.UTF8.GetByteCount(Content)), content);
    }

    [Fact]
    public void Read_WhenFileHasBom_GivesTheContentWithoutIt()
    {
        // Arrange
        string path = WriteFile("appsettings.json", Content, true);

        // Act
        (LocalStoredFileContent? content, string? problem) = LocalStoredFile.Read(path);

        // Assert
        Assert.Null(problem);
        Assert.Equal(
            new LocalStoredFileContent(Content, FakeSupportToolsServer.Sha256Of(Content),
                Encoding.UTF8.GetByteCount(Content)), content);
    }

    [Fact]
    public void Read_WhenFileIsEmpty_GivesAnEmptyContent()
    {
        // Arrange
        string path = WriteFile("empty.json", string.Empty, false);

        // Act
        (LocalStoredFileContent? content, _) = LocalStoredFile.Read(path);

        // Assert
        Assert.Equal(new LocalStoredFileContent(string.Empty, FakeSupportToolsServer.Sha256Of(string.Empty), 0),
            content);
    }

    //the limit counts the content without the BOM, as the server counts it; a file far over the limit is not read
    [Theory]
    [InlineData(StsStoredFileDataModel.ContentMaxBytes, false, null)]
    [InlineData(StsStoredFileDataModel.ContentMaxBytes, true, null)]
    [InlineData(StsStoredFileDataModel.ContentMaxBytes + 1, false, TooLargeProblem)]
    [InlineData(StsStoredFileDataModel.ContentMaxBytes + 1, true, TooLargeProblem)]
    [InlineData(StsStoredFileDataModel.ContentMaxBytes + 4, false, TooLargeProblem)]
    public void Read_ForFileSize_AcceptsContentUpToTheLimit(int length, bool withBom, string? expectedProblem)
    {
        // Arrange
        string text = new('a', length);
        string path = WriteFile("large.json", text, withBom);

        // Act
        (LocalStoredFileContent? content, string? problem) = LocalStoredFile.Read(path);

        // Assert
        Assert.Equal(expectedProblem, problem);
        Assert.Equal(expectedProblem is null ? FakeSupportToolsServer.Sha256Of(text) : null, content?.Sha256);
    }

    //binary content or another encoding would change on its way through the server, which stores text
    [Theory]
    [InlineData(new byte[] { 0x7B, 0x00, 0x7D })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x7B, 0x00, 0x7D, 0x00 })]
    [InlineData(new byte[] { 0x7B, 0x22, 0x43, 0x61, 0x66, 0xE9, 0x22, 0x7D })]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x7B, 0xC3, 0x28, 0x7D })]
    public async Task Read_WhenFileIsNotUtf8Text_ReportsIt(byte[] bytes)
    {
        // Arrange
        string path = Path.Combine(_folder, "binary.json");
        await File.WriteAllBytesAsync(path, bytes);

        // Act
        (LocalStoredFileContent? content, string? problem) = LocalStoredFile.Read(path);

        // Assert
        Assert.Null(content);
        Assert.Equal(NotTextProblem, problem);
    }

    [Fact]
    public void Read_WhenFileIsLocked_ReportsThatItCannotBeRead()
    {
        // Arrange
        string path = WriteFile("locked.json", Content, false);
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        // Act
        (LocalStoredFileContent? content, string? problem) = LocalStoredFile.Read(path);

        // Assert
        Assert.Null(content);
        Assert.StartsWith("cannot be read (", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_WhenFileDoesNotExist_ReportsThatItCannotBeRead()
    {
        // Act
        (LocalStoredFileContent? content, string? problem) = LocalStoredFile.Read(Path.Combine(_folder, "none.json"));

        // Assert
        Assert.Null(content);
        Assert.StartsWith("cannot be read (", problem, StringComparison.Ordinal);
    }

    //a new file is written in folders that do not exist yet, without a BOM
    [Fact]
    public async Task Write_WhenFolderDoesNotExist_CreatesItAndWritesWithoutBom()
    {
        // Arrange
        string path = Path.Combine(_folder, "AppFake", "dl360", "appsettings.json");

        // Act
        LocalStoredFile.Write(path, Content);

        // Assert
        Assert.Equal(Encoding.UTF8.GetBytes(Content), await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task Write_WhenExistingFileHasBom_KeepsIt()
    {
        // Arrange
        string path = WriteFile("appsettings.json", "{\"Old\":true}", true);

        // Act
        LocalStoredFile.Write(path, Content);

        // Assert
        byte[] expected = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Content)];
        Assert.Equal(expected, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task Write_WhenExistingFileHasNoBom_WritesNone()
    {
        // Arrange
        string path = WriteFile("appsettings.json", "{\"Old\":true,\"Longer\":\"than the new content\"}", false);

        // Act
        LocalStoredFile.Write(path, "{}");

        // Assert
        Assert.Equal(Encoding.UTF8.GetBytes("{}"), await File.ReadAllBytesAsync(path));
    }

    private string WriteFile(string name, string content, bool withBom)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllText(path, content, new UTF8Encoding(withBom));
        return path;
    }
}
