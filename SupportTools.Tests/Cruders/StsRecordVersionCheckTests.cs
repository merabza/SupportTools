using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Cruders;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Cruders;

//the version check of the Support Tools Server Editor (D4): the record is compared with the one of the loaded list
public sealed class StsRecordVersionCheckTests
{
    private const string EntityName = "Git";

    [Fact]
    public void FindVersion_WhenTheRecordIsListed_ReturnsItsVersionMatchingTheKeyIgnoringCase()
    {
        // Arrange
        List<FakeRecord> records = [new("RepoA", 3), new("RepoB", 5)];

        // Act
        int? result = StsRecordVersionCheck.FindVersion(records, "REPOB", x => x.Name, x => x.Version);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void FindVersion_WhenTheRecordIsNotListed_ReturnsNull()
    {
        // Arrange
        List<FakeRecord> records = [new("RepoA", 3)];

        // Act
        int? result = StsRecordVersionCheck.FindVersion(records, "RepoB", x => x.Name, x => x.Version);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Check_WhenTheServerHasTheLoadedVersion_AllowsTheWrite()
    {
        // Act
        Error? result = await Check("repoa", 3, new FakeRecord("RepoA", 3));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Check_WhenANewRecordIsNotOnTheServer_AllowsTheWrite()
    {
        // Act
        Error? result = await Check("RepoB", null, new FakeRecord("RepoA", 3));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Check_WhenTheServerVersionChanged_ReturnsAConflictWithBothVersions()
    {
        // Act
        Error? result = await Check("RepoA", 3, new FakeRecord("RepoA", 4));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ConcurrencyConflict", result.Code);
        Assert.Equal("Git RepoA Version Conflict: Expected 3, Actual 4", result.Description);
    }

    //a new name that another computer took meanwhile: the write would overwrite that record
    [Fact]
    public async Task Check_WhenANewRecordIsOnTheServerAlready_ReturnsAConflictExpectingNoRecord()
    {
        // Act
        Error? result = await Check("RepoA", null, new FakeRecord("RepoA", 2));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ConcurrencyConflict", result.Code);
        Assert.Equal("Git RepoA Version Conflict: Expected 0, Actual 2", result.Description);
    }

    [Fact]
    public async Task Check_WhenTheRecordWasDeletedOnTheServer_ReturnsNotFound()
    {
        // Act
        Error? result = await Check("RepoA", 3, new FakeRecord("RepoB", 3));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("RecordWithNameNotFound", result.Code);
        Assert.Equal("Git With Name RepoA Not Found", result.Description);
    }

    [Fact]
    public async Task Check_WhenTheServerListCannotBeRead_ReturnsItsErrorUnchanged()
    {
        // Arrange
        Error readError = Error.Problem("ApiRequestFailed", "the server does not answer");

        // Act
        Error? result = await StsRecordVersionCheck.Check(EntityName, "RepoA", 3,
            _ => Task.FromResult(Result.Failure<List<FakeRecord>>(readError)), x => x.Name, x => x.Version,
            CancellationToken.None);

        // Assert
        Assert.Same(readError, result);
    }

    [Fact]
    public async Task Check_WhenCalled_ReadsTheServerListWithTheCancellationToken()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        CancellationToken? received = null;

        // Act
        await StsRecordVersionCheck.Check(EntityName, "RepoA", null, cancellationToken =>
        {
            received = cancellationToken;
            return Task.FromResult(Result.Success<List<FakeRecord>>([]));
        }, x => x.Name, x => x.Version, cancellationTokenSource.Token);

        // Assert
        Assert.Equal(cancellationTokenSource.Token, received);
    }

    private static Task<Error?> Check(string key, int? loadedVersion, params FakeRecord[] serverRecords)
    {
        return StsRecordVersionCheck.Check(EntityName, key, loadedVersion,
            _ => Task.FromResult(Result.Success<List<FakeRecord>>([.. serverRecords])), x => x.Name,
            x => x.Version, CancellationToken.None);
    }

    private sealed record FakeRecord(string Name, int Version);
}
