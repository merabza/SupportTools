using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//a local record is a name of GitIgnorePatterns with the content of its file in the templates folder
[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitIgnorePatternsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = FakeSupportToolsServer.GitIgnoreFileTypes;
    private const string CSharpContent = "bin/\r\nobj/\r\n";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly string _folder;
    private readonly GitIgnorePatternsRegistrySyncAdapter _sut;

    public GitIgnorePatternsRegistrySyncAdapterTests()
    {
        _folder = Path.Combine(_context.TempFolder, "gitignore");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FileOf("CSharp"), CSharpContent);
        _context.Parameters.FolderForGitignoreFiles = _folder;
        _context.Parameters.GitIgnorePatterns.Add("CSharp");
        _sut = new GitIgnorePatternsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues()
    {
        // Assert
        Assert.Equal((RegistryCollections.GitIgnorePatterns, RegistryCollections.GitIgnorePatternsOrder),
            (_sut.CollectionName, _sut.Order));
    }

    //a template that this computer does not have yet is pulled
    [Fact]
    public async Task GetServerRecords_WhenTemplateIsOnlyOnServer_ReturnsIt()
    {
        // Arrange
        _context.Server.Store(Area, "React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "x" }, 2);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(2, Assert.Single(result.Value).Value.Version);
    }

    [Fact]
    public void ApplyLocal_WhenFolderIsNotSet_WritesNothing()
    {
        // Arrange
        _context.Parameters.FolderForGitignoreFiles = null;

        // Act
        _sut.ApplyLocal("React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "x" });

        // Assert
        Assert.Equal(["CSharp"], _context.Parameters.GitIgnorePatterns);
        Assert.False(File.Exists(FileOf("React")));
    }

    [Fact]
    public void GetLocalRecords_WhenNameIsListedTwice_GivesOneRecord()
    {
        // Arrange
        _context.Parameters.GitIgnorePatterns.Add("CSharp");

        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        Assert.Equal(["CSharp"], result.Keys);
    }

    [Fact]
    public void GetLocalRecords_WhenFileExists_GivesNameAndFileContent()
    {
        // Act
        IReadOnlyDictionary<string, object> result = _sut.GetLocalRecords();

        // Assert
        var record = Assert.IsType<StsGitIgnoreFileTypeDataModel>(result["CSharp"]);
        Assert.Equal("CSharp", record.Name);
        Assert.Equal(CSharpContent, record.Content);
        Assert.Empty(_context.Warnings.Items);
    }

    //a listed name without its file is an error of the local record: it is not synced at all, so the server record
    //is neither deleted as locally deleted nor pulled over the missing file
    [Fact]
    public async Task Records_WhenListedFileIsMissing_LeaveTheRecordOutOnBothSidesWithWarning()
    {
        // Arrange
        _context.Parameters.GitIgnorePatterns.Add("React");
        _context.Server.Store(Area, "React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "x" }, 2);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords =
            await _sut.GetServerRecords(default);
        IReadOnlyDictionary<string, object> localRecords = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Equal(["CSharp"], localRecords.Keys);
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal("React", warning.Key);
        Assert.Contains(FileOf("React"), warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Records_WhenFolderIsNotSet_SyncNothingWithWarning()
    {
        // Arrange
        _context.Parameters.FolderForGitignoreFiles = null;
        _context.Server.Store(Area, "React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "x" }, 2);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords =
            await _sut.GetServerRecords(default);
        IReadOnlyDictionary<string, object> localRecords = _sut.GetLocalRecords();

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Empty(localRecords);
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Null(warning.Key);
        Assert.Contains(nameof(SupportToolsParameters.FolderForGitignoreFiles), warning.Message,
            StringComparison.Ordinal);
    }

    //the server returns its own Id, which the client does not need
    [Fact]
    public async Task Normalize_WhenServerRecordHasId_GivesTheHashOfTheLocalRecord()
    {
        // Arrange
        _context.Server.Store(Area, "CSharp",
            new StsGitIgnoreFileTypeDataModel { Id = Guid.NewGuid(), Name = "CSharp", Content = CSharpContent }, 3);
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords =
            await _sut.GetServerRecords(default);

        // Act
        string serverHash = RegistryContractHasher.ComputeHash(_sut.Normalize(serverRecords.Value["CSharp"].Contract));
        string localHash = RegistryContractHasher.ComputeHash(_sut.Normalize(_sut.GetLocalRecords()["CSharp"]));

        // Assert
        Assert.Equal(localHash, serverHash);
    }

    [Fact]
    public void ApplyLocal_WhenTemplateIsNew_WritesTheFileIntoANewFolderAndAddsTheName()
    {
        // Arrange
        string folder = Path.Combine(_context.TempFolder, "new", "gitignore");
        _context.Parameters.FolderForGitignoreFiles = folder;

        // Act
        _sut.ApplyLocal("React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "node_modules/\n" });

        // Assert
        Assert.Equal("node_modules/\n",
            File.ReadAllText(SupportToolsParameters.GetGitIgnoreModelFilePath(folder, "React")));
        Assert.Equal(["CSharp", "React"], _context.Parameters.GitIgnorePatterns);
    }

    [Fact]
    public void ApplyLocal_WhenTemplateExists_OverwritesTheFileAndKeepsTheList()
    {
        // Act
        _sut.ApplyLocal("CSharp", new StsGitIgnoreFileTypeDataModel { Name = "csharp", Content = "new" });

        // Assert
        Assert.Equal("new", File.ReadAllText(FileOf("CSharp")));
        Assert.Equal(["CSharp"], _context.Parameters.GitIgnorePatterns);
    }

    //the user decided (C3) that the file stays on the disk; a file outside the list is not read
    [Fact]
    public void RemoveLocal_WhenCalled_RemovesTheNameAndKeepsTheFile()
    {
        // Act
        _sut.RemoveLocal("CSharp");

        // Assert
        Assert.Empty(_context.Parameters.GitIgnorePatterns);
        Assert.True(File.Exists(FileOf("CSharp")));
        Assert.Empty(_sut.GetLocalRecords());
    }

    [Fact]
    public async Task Upsert_WhenTemplateIsNew_UploadsOnlyItAndReturnsItsVersion()
    {
        // Arrange
        _context.Server.Store(Area, "React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "x" }, 2);

        // Act
        Result<int> result = await _sut.Upsert("CSharp", _sut.GetLocalRecords()["CSharp"], 0, default);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.Equal(CSharpContent, _context.Server.Get<StsGitIgnoreFileTypeDataModel>(Area, "CSharp")?.Content);
        Assert.Equal(2, _context.Server.VersionOf(Area, "React"));
        Assert.Equal(1, _context.Server.RequestCount("POST", "/git/syncupgitignorefiletypes/True"));
    }

    [Fact]
    public async Task Upsert_WhenServerHasAnotherVersion_ReturnsConcurrencyConflictWithoutWriting()
    {
        // Arrange
        _context.Server.Store(Area, "CSharp", new StsGitIgnoreFileTypeDataModel { Name = "CSharp", Content = "x" },
            5);

        // Act
        Result<int> result = await _sut.Upsert("CSharp", _sut.GetLocalRecords()["CSharp"], 4, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
        Assert.Equal(0, _context.Server.RequestCount("POST", "/git/syncupgitignorefiletypes"));
    }

    //the old endpoint answers GitIgnoreFileTypeWithNameNotFound
    [Fact]
    public async Task Delete_WhenRecordIsDeletedBetweenCheckAndDelete_ReturnsRecordWithNameNotFound()
    {
        // Arrange
        _context.Server.Store(Area, "React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "x" }, 2);
        _context.Server.BeforeRequest = request =>
        {
            if (request.StartsWith("DELETE", StringComparison.Ordinal))
            {
                _context.Server.Records(Area).Clear();
            }
        };

        // Act
        Result result = await _sut.Delete("React", 2, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RecordWithNameNotFound, result.Error.Code);
    }

    [Fact]
    public async Task Delete_WhenVersionIsExpected_DeletesTheRecord()
    {
        // Arrange
        _context.Server.Store(Area, "React", new StsGitIgnoreFileTypeDataModel { Name = "React", Content = "x" }, 2);

        // Act
        Result result = await _sut.Delete("React", 2, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Server.Records(Area));
    }

    private string FileOf(string name)
    {
        return SupportToolsParameters.GetGitIgnoreModelFilePath(_folder, name);
    }
}
