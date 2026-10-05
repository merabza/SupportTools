using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Sync;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsServerApiContracts.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

//the old git endpoints check no version (B1 left them unchanged): the adapter checks B1's rule against the server's
//list before it writes and reads the new version from the list afterwards
[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitsRegistrySyncAdapterTests : IDisposable
{
    private const string Area = FakeSupportToolsServer.GitRepos;
    private const string UpdateRequest = "POST /api/v1/git/updategitrepo/AppFront";
    private const string DeleteRequest = "DELETE /api/v1/git/deletegitrepo/AppFront";
    private readonly RegistryAdapterTestContext _context = new();
    private readonly GitsRegistrySyncAdapter _sut;

    public GitsRegistrySyncAdapterTests()
    {
        _sut = new GitsRegistrySyncAdapter(_context.ApiClient, _context.Parameters, _context.PathMapper,
            _context.Warnings);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Upsert_WhenRecordIsNew_WritesItAndReturnsItsVersion()
    {
        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("Front"), 0, default);

        // Assert
        Assert.Equal(1, result.Value);
        Assert.Equal("Front", _context.Server.Get<StsGitDataModel>(Area, "AppFront")?.GitProjectFolderName);
        Assert.Equal(1, _context.Server.RequestCount("POST", "/git/updategitrepo/AppFront"));
    }

    [Fact]
    public async Task Upsert_WhenVersionIsExpected_WritesAndReturnsTheNewVersion()
    {
        // Arrange
        _context.Server.Store(Area, "AppFront", NewContract("Old"), 3);

        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("New"), 3, default);

        // Assert
        Assert.Equal(4, result.Value);
        Assert.Equal("New", _context.Server.Get<StsGitDataModel>(Area, "AppFront")?.GitProjectFolderName);
    }

    [Fact]
    public async Task Upsert_WhenServerHasAnotherVersion_ReturnsConcurrencyConflictWithoutWriting()
    {
        // Arrange
        _context.Server.Store(Area, "AppFront", NewContract("Old"), 4);

        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("New"), 3, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
        Assert.DoesNotContain(UpdateRequest, _context.Server.Requests);
    }

    //expected version 0: the record must not exist on the server
    [Fact]
    public async Task Upsert_WhenCreatedRecordAlreadyExists_ReturnsConcurrencyConflictWithoutWriting()
    {
        // Arrange
        _context.Server.Store(Area, "appfront", NewContract("Old"), 1);

        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("New"), 0, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
        Assert.DoesNotContain(UpdateRequest, _context.Server.Requests);
    }

    [Fact]
    public async Task Upsert_WhenUpdatedRecordWasDeletedOnServer_ReturnsRecordWithNameNotFoundWithoutWriting()
    {
        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("New"), 3, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RecordWithNameNotFound, result.Error.Code);
        Assert.DoesNotContain(UpdateRequest, _context.Server.Requests);
    }

    [Fact]
    public async Task Upsert_WhenServerIsUnavailable_ReturnsRequestFailed()
    {
        // Arrange
        _context.Server.IsUnavailable = true;

        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("New"), 0, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
    }

    [Fact]
    public async Task Delete_WhenVersionIsExpected_DeletesTheRecord()
    {
        // Arrange
        _context.Server.Store(Area, "AppFront", NewContract("Front"), 2);

        // Act
        Result result = await _sut.Delete("AppFront", 2, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Server.Records(Area));
    }

    [Fact]
    public async Task Delete_WhenServerHasAnotherVersion_ReturnsConcurrencyConflictWithoutDeleting()
    {
        // Arrange
        _context.Server.Store(Area, "AppFront", NewContract("Front"), 3);

        // Act
        Result result = await _sut.Delete("AppFront", 2, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.ConcurrencyConflict, result.Error.Code);
        Assert.DoesNotContain(DeleteRequest, _context.Server.Requests);
    }

    //the engine treats RecordWithNameNotFound of a delete as done: the record is gone anyway
    [Fact]
    public async Task Delete_WhenRecordIsAlreadyGone_ReturnsRecordWithNameNotFound()
    {
        // Act
        Result result = await _sut.Delete("AppFront", 2, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RecordWithNameNotFound, result.Error.Code);
    }

    //the old endpoint answers GitWithKeyNotFound
    [Fact]
    public async Task Delete_WhenRecordIsDeletedBetweenCheckAndDelete_ReturnsRecordWithNameNotFound()
    {
        // Arrange
        _context.Server.Store(Area, "AppFront", NewContract("Front"), 2);
        _context.Server.BeforeRequest = request =>
        {
            if (request == DeleteRequest)
            {
                _context.Server.Records(Area).Clear();
            }
        };

        // Act
        Result result = await _sut.Delete("AppFront", 2, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RecordWithNameNotFound, result.Error.Code);
    }

    [Fact]
    public void CollectionNameAndOrder_WhenRead_AreTheRegistryCollectionValues()
    {
        // Assert
        Assert.Equal((RegistryCollections.Gits, RegistryCollections.GitsOrder), (_sut.CollectionName, _sut.Order));
    }

    //e.g. the server does not know the gitignore type of the git
    [Fact]
    public async Task Upsert_WhenServerRefusesTheWrite_ReturnsItsError()
    {
        // Arrange
        _context.Server.Fail(UpdateRequest, HttpStatusCode.NotFound, "GitIgnoreFileTypeWithNameNotFound");

        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("Front"), 0, default);

        // Assert
        Assert.Equal("GitIgnoreFileTypeWithNameNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Upsert_WhenNewVersionCannotBeRead_ReturnsRequestFailed()
    {
        // Arrange
        _context.Server.BeforeRequest = request =>
        {
            if (request == UpdateRequest)
            {
                _context.Server.IsUnavailable = true;
            }
        };

        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("Front"), 0, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
        Assert.Equal(1, _context.Server.VersionOf(Area, "AppFront"));
    }

    //another computer deleted the record right after the write
    [Fact]
    public async Task Upsert_WhenRecordIsGoneAfterTheWrite_ReturnsRecordWithNameNotFound()
    {
        // Arrange
        int listRequests = 0;
        _context.Server.BeforeRequest = request =>
        {
            if (request == "GET /api/v1/git/gitrepos" && ++listRequests == 2)
            {
                _context.Server.Records(Area).Clear();
            }
        };

        // Act
        Result<int> result = await _sut.Upsert("AppFront", NewContract("Front"), 0, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RecordWithNameNotFound, result.Error.Code);
    }

    //e.g. a project uses the git (B6): the engine reports it as failed
    [Fact]
    public async Task Delete_WhenServerRefusesTheDelete_ReturnsItsError()
    {
        // Arrange
        _context.Server.Store(Area, "AppFront", NewContract("Front"), 2);
        _context.Server.Fail(DeleteRequest, HttpStatusCode.Conflict, "RecordIsInUse");

        // Act
        Result result = await _sut.Delete("AppFront", 2, default);

        // Assert
        Assert.Equal("RecordIsInUse", result.Error.Code);
    }

    [Fact]
    public async Task Delete_WhenServerIsUnavailable_ReturnsRequestFailed()
    {
        // Arrange
        _context.Server.IsUnavailable = true;

        // Act
        Result result = await _sut.Delete("AppFront", 2, default);

        // Assert
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
    }

    [Fact]
    public void ApplyLocal_WhenRecordExists_UpdatesTheSameObject()
    {
        // Arrange
        var existing = new GitDataModel { GitProjectFolderName = "Old" };
        _context.Parameters.Gits["AppFront"] = existing;

        // Act
        _sut.ApplyLocal("AppFront", NewContract("New"));

        // Assert
        Assert.Same(existing, _context.Parameters.Gits["AppFront"]);
        Assert.Equal("New", existing.GitProjectFolderName);
        Assert.Equal("React", existing.GitIgnorePatternName);
    }

    [Fact]
    public void ApplyLocal_WhenRecordIsNew_AddsIt()
    {
        // Act
        _sut.ApplyLocal("AppFront", NewContract("Front"));

        // Assert
        Assert.Equal("git@github.com:fake/AppFront.git", _context.Parameters.Gits["AppFront"].GitProjectAddress);
    }

    [Fact]
    public void RemoveLocal_WhenRecordExists_RemovesIt()
    {
        // Arrange
        _context.Parameters.Gits["AppFront"] = new GitDataModel();
        _context.Parameters.Gits["AppBack"] = new GitDataModel();

        // Act
        _sut.RemoveLocal("AppFront");

        // Assert
        Assert.Equal(["AppBack"], _context.Parameters.Gits.Keys);
    }

    [Fact]
    public void Normalize_WhenCalled_KeepsTheContent()
    {
        // Arrange
        StsGitDataModel contract = NewContract("Front");

        // Act
        object result = _sut.Normalize(contract);

        // Assert
        Assert.Equal(RegistryContractHasher.ComputeHash(NewContract("Front")),
            RegistryContractHasher.ComputeHash(result));
    }

    [Fact]
    public async Task GetServerRecords_WhenServerHasRecords_ReturnsThemWithTheirVersions()
    {
        // Arrange
        _context.Server.Store(Area, "AppFront", NewContract("Front"), 5);

        // Act
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> result = await _sut.GetServerRecords(default);

        // Assert
        Assert.Equal(5, result.Value["AppFront"].Version);
    }

    [Fact]
    public void GetLocalRecords_WhenComputerIsLinux_GivesCanonicalFolderName()
    {
        // Arrange
        var sut = new GitsRegistrySyncAdapter(_context.ApiClient, _context.Parameters,
            MapperTestHelpers.LinuxPathMapper(), _context.Warnings);
        _context.Parameters.Gits["AppFront"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:fake/AppFront.git",
            GitProjectFolderName = "Front/app",
            GitIgnorePatternName = "React"
        };

        // Act
        IReadOnlyDictionary<string, object> result = sut.GetLocalRecords();

        // Assert
        Assert.Equal(@"Front\app", Assert.IsType<StsGitDataModel>(result["AppFront"]).GitProjectFolderName);
    }

    private static StsGitDataModel NewContract(string folderName)
    {
        return new StsGitDataModel
        {
            GitProjectName = "AppFront",
            GitProjectAddress = "git@github.com:fake/AppFront.git",
            GitProjectFolderName = folderName,
            GitIgnorePatternName = "React"
        };
    }
}
