using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the stored files of the registry (C6) against the fake server. The canonical D:\1WorkSecurity is a temp folder of
//this computer. A local record is an existing file that the registry points to, that the server has or that was
//synced before; its key is the canonical path. All contents are made up
[Collection(ConsoleCaptureCollection.Name)]
public sealed class StoredFilesRegistrySyncAdapterTests : IDisposable
{
    private const string CanonicalRoot = @"D:\1WorkSecurity";
    private const string AppSettings = @"AppFake\PAZISI\appsettings.json";
    private const string Content = "{\"ConnectionString\":\"fake-connection-0001\",\"Name\":\"ფაილი\"}\r\n";
    private const string ServerContent = "{\"ConnectionString\":\"fake-server-connection-0002\"}\r\n";
    private const string NotSyncedSuffix = ", the file is not synced";

    private readonly RegistryAdapterTestContext _context = new();
    private readonly string _localRoot;
    private readonly ProjectModel _project = new();

    public StoredFilesRegistrySyncAdapterTests()
    {
        _localRoot = Path.Combine(_context.TempFolder, "1WorkSecurity");
        _context.PathMapper = new PathMapper(
            [new PathMappingModel { CanonicalPrefix = CanonicalRoot, LocalPrefix = _localRoot }], '\\');
        _context.Parameters.Projects["AppFake"] = _project;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues()
    {
        // Act
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Assert
        Assert.Equal((RegistryCollections.StoredFiles, RegistryCollections.StoredFilesOrder),
            (sut.CollectionName, sut.Order));
    }

    //the file fields of the registry; the generated AppSettingsEncodedJsonFileName and empty values are not files
    [Fact]
    public void GetLocalRecords_WhenRegistryPointsToFiles_GivesEveryExistingFileByItsCanonicalPath()
    {
        // Arrange
        _project.SeedProjectParametersFilePath = WriteLocal(@"AppFake\Seed.json", "{}");
        _project.PrepareProdCopyDatabaseProjectParametersFilePath = WriteLocal(@"AppFake\Prepare.json", "{}");
        _project.PairedDbObjectsResultFileName = WriteLocal(@"AppFake\Paired.json", "{}");
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(AppSettings, Content),
            AppSettingsEncodedJsonFileName = WriteLocal(@"AppFake\PAZISI\appsettingsEncoded.json", "{}")
        };
        _project.ServerInfos["two"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(@"AppFake\Merinson\appsettings.json", "{}")
        };
        _context.Parameters.Projects["AppEmpty"] = new ProjectModel
        {
            SeedProjectParametersFilePath = " ", PairedDbObjectsResultFileName = string.Empty
        };

        // Act
        IReadOnlyDictionary<string, object> result = CreateSut().GetLocalRecords();

        // Assert
        string[] expected =
        [
            KeyOf(@"AppFake\Merinson\appsettings.json"), KeyOf(AppSettings), KeyOf(@"AppFake\Paired.json"),
            KeyOf(@"AppFake\Prepare.json"), KeyOf(@"AppFake\Seed.json")
        ];
        Assert.Equal(expected, result.Keys.Order(StringComparer.Ordinal));
        var contract = Assert.IsType<StoredFileContract>(result[KeyOf(AppSettings)]);
        Assert.Equal(KeyOf(AppSettings), contract.Path);
        Assert.Equal(FakeSupportToolsServer.Sha256Of(Content), contract.Sha256);
        Assert.Equal(Encoding.UTF8.GetByteCount(Content), contract.Length);
        Assert.Empty(_context.Warnings.Items);
    }

    //Windows paths ignore case: paths that differ only by case are one file and keep the first spelling; the drive
    //letter of a canonical path is upper case
    [Fact]
    public void GetLocalRecords_WhenPathsDifferOnlyByCase_GivesOneRecordWithTheFirstSpelling()
    {
        // Arrange
        _context.PathMapper = MapperTestHelpers.WindowsPathMapper();
        string path = WriteLocal(AppSettings, Content);
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = char.ToLowerInvariant(path[0]) + path[1..]
        };
        _project.ServerInfos["two"] = new ServerInfoModel { AppSettingsJsonSourceFileName = path.ToUpperInvariant() };

        // Act
        IReadOnlyDictionary<string, object> result = CreateSut().GetLocalRecords();

        // Assert
        Assert.Equal([char.ToUpperInvariant(path[0]) + path[1..]], result.Keys);
    }

    //the BOM is not part of the content: the hash and the length are those of the content, as the server counts them
    [Fact]
    public void GetLocalRecords_WhenFileHasBom_HashesTheContentWithoutIt()
    {
        // Arrange
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(AppSettings, Content, true)
        };

        // Act
        IReadOnlyDictionary<string, object> result = CreateSut().GetLocalRecords();

        // Assert
        var contract = Assert.IsType<StoredFileContract>(Assert.Single(result).Value);
        Assert.Equal((FakeSupportToolsServer.Sha256Of(Content), Encoding.UTF8.GetByteCount(Content)),
            (contract.Sha256, contract.Length));
    }

    //a file that the registry points to but that is not here belongs here: the engine takes it from the server
    [Fact]
    public void GetMissingLocalKeys_WhenReferencedFileDoesNotExist_GivesItWithoutWarning()
    {
        // Arrange
        _project.ServerInfos["one"] = new ServerInfoModel { AppSettingsJsonSourceFileName = LocalOf(AppSettings) };
        _project.ServerInfos["two"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(@"AppFake\Merinson\appsettings.json", "{}")
        };
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Act
        IReadOnlyCollection<string> result = sut.GetMissingLocalKeys();

        // Assert
        Assert.Equal([KeyOf(AppSettings)], result);
        Assert.Equal([KeyOf(@"AppFake\Merinson\appsettings.json")], sut.GetLocalRecords().Keys);
        Assert.Empty(_context.Warnings.Items);
    }

    //git carries the files of its working trees: such a file is left out on both sides, without a warning
    [Fact]
    public async Task Records_WhenFileIsInAGitWorkingTree_LeaveItOutOnBothSidesSilently()
    {
        // Arrange
        const string inRepository = @"AppRepo\AppRepo\appsettings.json";
        Directory.CreateDirectory(LocalOf(@"AppRepo\.git"));
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(inRepository, Content)
        };
        _project.ServerInfos["two"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = LocalOf(@"AppRepo\Missing\appsettings.json")
        };
        _context.Server.StoreFile(KeyOf(inRepository), ServerContent, 1);
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Empty(sut.GetLocalRecords());
        Assert.Empty(sut.GetMissingLocalKeys());
        Assert.Empty(_context.Warnings.Items);
    }

    //a git worktree or submodule has a .git file instead of a folder
    [Fact]
    public void GetLocalRecords_WhenFileIsInAGitWorktree_LeavesItOutSilently()
    {
        // Arrange
        WriteLocal(@"AppRepo\.git", "gitdir: D:/1WorkDotnet/AppRepo/.git/worktrees/AppRepo");
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(@"AppRepo\App\appsettings.json", Content)
        };

        // Act
        IReadOnlyDictionary<string, object> result = CreateSut().GetLocalRecords();

        // Assert
        Assert.Empty(result);
        Assert.Empty(_context.Warnings.Items);
    }

    //a local form that is not a full path on this computer (here a relative local prefix) cannot be written
    [Fact]
    public async Task Records_WhenLocalFormIsNotAFullPath_LeaveItOutWithWarning()
    {
        // Arrange
        _context.PathMapper = new PathMapper(
            [new PathMappingModel { CanonicalPrefix = CanonicalRoot, LocalPrefix = @"relative\1WorkSecurity" }], '\\');
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 1);
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);

        // Assert
        Assert.Empty(serverRecords.Value);
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal("The path has no form on this computer (add a path mapping)" + NotSyncedSuffix, warning.Message);
        Assert.Empty(_context.PathMapper.Issues);
    }

    [Fact]
    public async Task GetServerRecords_WhenServerIsUnreachable_ReturnsTheTransportError()
    {
        // Arrange
        _context.Server.IsUnavailable = true;

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await CreateSut().GetServerRecords(default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
    }

    //the server takes UTF-8 text up to ContentMaxBytes; the BOM is not part of the content
    [Theory]
    [InlineData(0, false, true)]
    [InlineData(StsStoredFileDataModel.ContentMaxBytes, true, true)]
    [InlineData(StsStoredFileDataModel.ContentMaxBytes + 1, false, false)]
    public void GetLocalRecords_ForFileSize_SyncsOnlyFilesUpToTheLimit(int length, bool withBom, bool isSynced)
    {
        // Arrange
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(AppSettings, new string('a', length), withBom)
        };

        // Act
        IReadOnlyDictionary<string, object> result = CreateSut().GetLocalRecords();

        // Assert
        Assert.Equal(isSynced, result.ContainsKey(KeyOf(AppSettings)));
        Assert.Equal(isSynced, _context.Warnings.Items.Count == 0);
    }

    //a file that is too large or is not UTF-8 text is left out on both sides with a warning, so the server record is
    //neither overwritten nor taken over the local file
    [Theory]
    [InlineData(new byte[] { 0x7B, 0x00, 0x7D }, "is not a UTF-8 text file")]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x7B, 0x00, 0x7D, 0x00 }, "is not a UTF-8 text file")]
    [InlineData(new byte[] { 0x7B, 0x22, 0x43, 0x61, 0x66, 0xE9, 0x22, 0x7D }, "is not a UTF-8 text file")]
    [InlineData(null, "is larger than 1048576 bytes")]
    public async Task Records_WhenFileCannotBeSynced_LeaveItOutOnBothSidesWithWarning(byte[]? bytes,
        string expectedProblem)
    {
        // Arrange
        string path = LocalOf(AppSettings);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path,
            bytes ?? Encoding.UTF8.GetBytes(new string('a', StsStoredFileDataModel.ContentMaxBytes + 1)));
        _project.ServerInfos["one"] = new ServerInfoModel { AppSettingsJsonSourceFileName = path };
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 1);
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);
        IReadOnlyDictionary<string, object> localRecords = sut.GetLocalRecords();

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Empty(localRecords);
        Assert.Empty(sut.GetMissingLocalKeys());
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal((RegistryCollections.StoredFiles, KeyOf(AppSettings)), (warning.CollectionName, warning.Key));
        Assert.Equal($"{path} {expectedProblem}{NotSyncedSuffix}", warning.Message);
    }

    //a registry path that the server does not take (relative, network, an empty segment) is left out with a warning
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData(@"\\files.example.test\share\appsettings.json")]
    [InlineData(@"<root>\AppFake\\appsettings.json")]
    public void Records_WhenPathIsNotACanonicalFilePath_LeaveItOutWithWarning(string path)
    {
        // Arrange
        string localPath = path.Replace("<root>", _localRoot, StringComparison.Ordinal);
        _project.ServerInfos["one"] = new ServerInfoModel { AppSettingsJsonSourceFileName = localPath };
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Act
        IReadOnlyDictionary<string, object> result = sut.GetLocalRecords();

        // Assert
        Assert.Empty(result);
        Assert.Empty(sut.GetMissingLocalKeys());
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal(@"The path is not a canonical file path (X:\...) that the server accepts" + NotSyncedSuffix,
            warning.Message);
    }

    //on Linux the registry path gives the canonical key back, and a taken file is written in the local form, with the
    //folders that do not exist yet
    [Fact]
    public async Task PrepareAndApplyLocal_OnLinux_WriteTheFileInItsLocalFormCreatingTheFolders()
    {
        // Arrange
        string linuxRoot = $"{_context.TempFolder.Replace('\\', '/')}/linux/1WorkSecurity";
        _context.PathMapper = new PathMapper(
            [new PathMappingModel { CanonicalPrefix = CanonicalRoot, LocalPrefix = linuxRoot }], '/');
        string linuxPath = $"{linuxRoot}/AppFake/PAZISI/appsettings.json";
        _project.ServerInfos["one"] = new ServerInfoModel { AppSettingsJsonSourceFileName = linuxPath };
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 3);
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);
        object contract = serverRecords.Value[KeyOf(AppSettings)].Contract;

        // Act
        Result prepared = await sut.PrepareApplyLocal(KeyOf(AppSettings), contract, default);
        sut.ApplyLocal(KeyOf(AppSettings), contract);

        // Assert
        Assert.True(prepared.IsSuccess);
        Assert.Equal(Encoding.UTF8.GetBytes(ServerContent), await File.ReadAllBytesAsync(linuxPath));
        Assert.Equal([KeyOf(AppSettings)], sut.GetLocalRecords().Keys);
        Assert.Empty(sut.GetMissingLocalKeys());
        Assert.Empty(_context.Warnings.Items);
        Assert.Empty(_context.PathMapper.Issues);
    }

    //a server file whose canonical path has no form on this computer is left out on both sides with a warning
    [Fact]
    public async Task Records_WhenServerPathHasNoLocalForm_LeaveItOutWithWarning()
    {
        // Arrange
        const string unmapped = @"D:\ProgData\SupportToolsData\Paired.json";
        _context.PathMapper = MapperTestHelpers.LinuxPathMapper();
        _context.Server.StoreFile(unmapped, ServerContent, 1);
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);

        // Assert
        Assert.Empty(serverRecords.Value);
        Assert.Empty(sut.GetLocalRecords());
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.Equal(unmapped, warning.Key);
        Assert.Equal("The path has no form on this computer (add a path mapping)" + NotSyncedSuffix, warning.Message);
    }

    //the server list has no content, so reading the server records asks for nothing else
    [Fact]
    public async Task GetServerRecords_WhenServerHasFiles_GivesTheirMetadataWithoutAskingForContent()
    {
        // Arrange
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 3);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await CreateSut().GetServerRecords(default);

        // Assert
        RegistryServerRecord record = Assert.Single(result.Value).Value;
        Assert.Equal(3, record.Version);
        var contract = Assert.IsType<StoredFileContract>(record.Contract);
        Assert.Equal(
            (KeyOf(AppSettings), FakeSupportToolsServer.Sha256Of(ServerContent),
                Encoding.UTF8.GetByteCount(ServerContent)), (contract.Path, contract.Sha256, contract.Length));
        Assert.Equal(["GET /api/v1/files"], _context.Server.Requests);
    }

    //only the content of the file to take is asked for, and a new file is written without a BOM
    [Fact]
    public async Task PrepareAndApplyLocal_WhenFileIsTaken_AskForItsContentOnlyAndWriteIt()
    {
        // Arrange
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 3);
        _context.Server.StoreFile(KeyOf(@"AppOther\appsettings.json"), Content, 1);
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);
        object contract = serverRecords.Value[KeyOf(AppSettings)].Contract;

        // Act
        Result prepared = await sut.PrepareApplyLocal(KeyOf(AppSettings), contract, default);
        sut.ApplyLocal(KeyOf(AppSettings), contract);

        // Assert
        Assert.True(prepared.IsSuccess);
        Assert.Equal(1, _context.Server.RequestCount("GET", "/files/content"));
        Assert.DoesNotContain(_context.Server.Requests, x => x.Contains("AppOther", StringComparison.Ordinal));
        Assert.Equal(Encoding.UTF8.GetBytes(ServerContent), await File.ReadAllBytesAsync(LocalOf(AppSettings)));
    }

    [Fact]
    public async Task PrepareApplyLocal_WhenFileIsGoneFromServer_ReturnsTheServerError()
    {
        // Arrange
        var contract = new StoredFileContract { Path = KeyOf(AppSettings), Sha256 = "AB", Length = 2, Version = 1 };

        // Act
        Result result = await CreateSut().PrepareApplyLocal(KeyOf(AppSettings), contract, default);

        // Assert
        Assert.Equal("RecordWithNameNotFound", result.Error.Code);
    }

    //the content must come from the server first; without it nothing would change and the old file would look taken
    [Fact]
    public void ApplyLocal_WhenContentWasNotTaken_Throws()
    {
        // Arrange
        var contract = new StoredFileContract { Path = KeyOf(AppSettings), Sha256 = "AB", Length = 2, Version = 1 };
        StoredFilesRegistrySyncAdapter sut = CreateSut();

        // Act
        Exception? exception = Record.Exception(() => sut.ApplyLocal(KeyOf(AppSettings), contract));

        // Assert
        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.EndsWith("PrepareApplyLocal must run first", invalidOperation.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(LocalOf(AppSettings)));
    }

    //IsSynced keeps a file without a local form out of the plan, so only a direct call gets this far
    [Fact]
    public async Task ApplyLocal_WhenKeyHasNoLocalForm_Throws()
    {
        // Arrange
        const string unmapped = @"D:\ProgData\SupportToolsData\Paired.json";
        _context.PathMapper = MapperTestHelpers.LinuxPathMapper();
        _context.Server.StoreFile(unmapped, ServerContent, 1);
        var contract = new StoredFileContract
        {
            Path = unmapped,
            Sha256 = FakeSupportToolsServer.Sha256Of(ServerContent),
            Length = Encoding.UTF8.GetByteCount(ServerContent),
            Version = 1
        };
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        await sut.PrepareApplyLocal(unmapped, contract, default);

        // Act
        Exception? exception = Record.Exception(() => sut.ApplyLocal(unmapped, contract));

        // Assert
        var invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.EndsWith("no local path", invalidOperation.Message, StringComparison.Ordinal);
    }

    //the BOM of an existing file stays, so only the content changes
    [Fact]
    public async Task ApplyLocal_WhenExistingFileHasBom_KeepsIt()
    {
        // Arrange
        string path = WriteLocal(AppSettings, Content, true);
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 2);
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);
        object contract = serverRecords.Value[KeyOf(AppSettings)].Contract;
        await sut.PrepareApplyLocal(KeyOf(AppSettings), contract, default);

        // Act
        sut.ApplyLocal(KeyOf(AppSettings), contract);

        // Assert
        byte[] expected = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(ServerContent)];
        Assert.Equal(expected, await File.ReadAllBytesAsync(path));
    }

    //the server computes the hash of the stored content like the adapter computes it for the local file (B8), so
    //after the upload both sides are equal
    [Fact]
    public async Task Upsert_WhenFileIsReadable_SendsItsContentAndBothSidesHashEqually()
    {
        // Arrange
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(AppSettings, Content, true)
        };
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        object local = sut.GetLocalRecords()[KeyOf(AppSettings)];

        // Act
        Result<int> result = await sut.Upsert(KeyOf(AppSettings), local, 0, default);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.Equal(Content, _context.Server.FileContent(KeyOf(AppSettings)));
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecords = await sut.GetServerRecords(default);
        Assert.Equal(RegistryContractHasher.ComputeHash(sut.Normalize(local)),
            RegistryContractHasher.ComputeHash(sut.Normalize(serverRecords.Value[KeyOf(AppSettings)].Contract)));
    }

    [Fact]
    public async Task Upsert_WhenServerVersionMovedOn_ReturnsTheConflict()
    {
        // Arrange
        _project.ServerInfos["one"] = new ServerInfoModel
        {
            AppSettingsJsonSourceFileName = WriteLocal(AppSettings, Content)
        };
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 2);
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        object local = sut.GetLocalRecords()[KeyOf(AppSettings)];

        // Act
        Result<int> result = await sut.Upsert(KeyOf(AppSettings), local, 1, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
        Assert.Equal(ServerContent, _context.Server.FileContent(KeyOf(AppSettings)));
    }

    //a file that cannot be read any more is not sent
    [Fact]
    public async Task Upsert_WhenFileWasDeletedAfterThePlan_DoesNotSendIt()
    {
        // Arrange
        string path = WriteLocal(AppSettings, Content);
        _project.ServerInfos["one"] = new ServerInfoModel { AppSettingsJsonSourceFileName = path };
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        object local = sut.GetLocalRecords()[KeyOf(AppSettings)];
        File.Delete(path);

        // Act
        Result<int> result = await sut.Upsert(KeyOf(AppSettings), local, 0, default);

        // Assert
        Assert.Equal(nameof(RegistrySyncErrors.LocalRecordIsInvalid), result.Error.Code);
        Assert.Contains($"{path} does not exist", result.Error.Description, StringComparison.Ordinal);
        Assert.Empty(_context.Server.Requests);
    }

    //a file of a git working tree is never a local record, so an upload of it names the reason
    [Fact]
    public async Task Upsert_WhenFileIsInAGitWorkingTree_DoesNotSendIt()
    {
        // Arrange
        const string inRepository = @"AppRepo\AppRepo\appsettings.json";
        Directory.CreateDirectory(LocalOf(@"AppRepo\.git"));
        WriteLocal(inRepository, Content);
        var contract = new StoredFileContract
        {
            Path = KeyOf(inRepository),
            Sha256 = FakeSupportToolsServer.Sha256Of(Content),
            Length = Encoding.UTF8.GetByteCount(Content)
        };

        // Act
        Result<int> result = await CreateSut().Upsert(KeyOf(inRepository), contract, 0, default);

        // Assert
        Assert.Equal(nameof(RegistrySyncErrors.LocalRecordIsInvalid), result.Error.Code);
        Assert.EndsWith($"{LocalOf(inRepository)} is in a git working tree", result.Error.Description,
            StringComparison.Ordinal);
        Assert.Empty(_context.Server.Requests);
    }

    [Fact]
    public async Task Delete_WhenVersionMatches_DeletesTheFileOnTheServer()
    {
        // Arrange
        _context.Server.StoreFile(KeyOf(AppSettings), ServerContent, 4);

        // Act
        Result result = await CreateSut().Delete(KeyOf(AppSettings), 4, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(_context.Server.FileContent(KeyOf(AppSettings)));
        Assert.Contains(_context.Server.Requests,
            x => x.StartsWith("DELETE /api/v1/files/delete?path=", StringComparison.Ordinal) &&
                 x.EndsWith("&version=4", StringComparison.Ordinal));
    }

    //RemoveLocal runs only for a delete that the user confirmed
    [Fact]
    public void RemoveLocal_WhenFileExists_DeletesIt()
    {
        // Arrange
        string path = WriteLocal(AppSettings, Content);

        // Act
        CreateSut().RemoveLocal(KeyOf(AppSettings));

        // Assert
        Assert.False(File.Exists(path));
        Assert.Empty(_context.Warnings.Items);
    }

    //the file stays when it cannot be deleted: the engine then reports the record as Failed, and the warning says why
    [Fact]
    public void RemoveLocal_WhenFileCannotBeDeleted_KeepsItAndWarns()
    {
        // Arrange
        string path = WriteLocal(AppSettings, Content);
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        // Act
        CreateSut().RemoveLocal(KeyOf(AppSettings));

        // Assert
        Assert.True(File.Exists(path));
        RegistrySyncWarning warning = Assert.Single(_context.Warnings.Items);
        Assert.StartsWith($"{path} cannot be deleted (", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemoveLocal_WhenFileIsMissing_DoesNothing()
    {
        // Act
        CreateSut().RemoveLocal(KeyOf(AppSettings));

        // Assert
        Assert.False(File.Exists(LocalOf(AppSettings)));
        Assert.Empty(_context.Warnings.Items);
    }

    //file-based deletion (C6): a file stays in the sync while it exists, even when the registry no longer points to
    //it; an unknown file that nothing points to is not a record
    [Fact]
    public async Task GetLocalRecords_WhenUnreferencedFileIsOnServerOrWasSynced_GivesIt()
    {
        // Arrange
        WriteLocal(AppSettings, Content);
        WriteLocal(@"AppOld\appsettings.json", Content);
        WriteLocal(@"AppStray\appsettings.json", Content);
        _context.Server.StoreFile(KeyOf(AppSettings), Content, 1);
        _context.Parameters.RegistrySyncState.GetOrAddCollection(RegistryCollections.StoredFiles)
            .Records[KeyOf(@"AppOld\appsettings.json")] = new RegistryRecordSyncStateModel
        {
            Version = 2, Hash = "FAKEHASH"
        };
        StoredFilesRegistrySyncAdapter sut = CreateSut();
        await sut.GetServerRecords(default);

        // Act
        IReadOnlyDictionary<string, object> result = sut.GetLocalRecords();

        // Assert
        Assert.Equal([KeyOf(AppSettings), KeyOf(@"AppOld\appsettings.json")],
            result.Keys.Order(StringComparer.Ordinal));
        Assert.Empty(sut.GetMissingLocalKeys());
    }

    private StoredFilesRegistrySyncAdapter CreateSut()
    {
        return new StoredFilesRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.PathMapper,
            _context.Warnings);
    }

    private static string KeyOf(string relativePath)
    {
        return $@"{CanonicalRoot}\{relativePath}";
    }

    private string LocalOf(string relativePath)
    {
        return Path.Combine(_localRoot, relativePath);
    }

    private string WriteLocal(string relativePath, string content, bool withBom = false)
    {
        string path = LocalOf(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(withBom));
        return path;
    }
}
