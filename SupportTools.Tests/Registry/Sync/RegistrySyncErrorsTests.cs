using LibSupportToolsServerWork.Registry.Sync;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

//the sync command (C5) shows these descriptions, so they name the collection and the record
public sealed class RegistrySyncErrorsTests
{
    [Fact]
    public void DuplicateKeys_WhenCalled_NamesCollectionSideAndKeys()
    {
        // Act
        Error result = RegistrySyncErrors.DuplicateKeys("Projects", "server", "App/APP");

        // Assert
        Assert.Equal("DuplicateKeys", result.Code);
        Assert.Equal("Projects: server keys differ only by case: App/APP", result.Description);
        Assert.Equal(ErrorType.Problem, result.Type);
    }

    [Fact]
    public void NormalizationIsNotStable_WhenCalled_NamesCollectionAndKey()
    {
        // Act
        Error result = RegistrySyncErrors.NormalizationIsNotStable("Projects", "App");

        // Assert
        Assert.Equal("NormalizationIsNotStable", result.Code);
        Assert.Equal("Projects/App: normalizing the normalized contract again changes its hash", result.Description);
        Assert.Equal(ErrorType.Problem, result.Type);
    }

    [Fact]
    public void LocalRecordNotApplied_WhenCalled_NamesCollectionAndKey()
    {
        // Act
        Error result = RegistrySyncErrors.LocalRecordNotApplied("Projects", "App");

        // Assert
        Assert.Equal("LocalRecordNotApplied", result.Code);
        Assert.Equal("Projects/App: the record is not in the local records after it was applied",
            result.Description);
        Assert.Equal(ErrorType.Problem, result.Type);
    }

    [Fact]
    public void LocalRecordNotRemoved_WhenCalled_NamesCollectionAndKey()
    {
        // Act
        Error result = RegistrySyncErrors.LocalRecordNotRemoved("Projects", "App");

        // Assert
        Assert.Equal("LocalRecordNotRemoved", result.Code);
        Assert.Equal("Projects/App: the record is still in the local records after it was removed",
            result.Description);
        Assert.Equal(ErrorType.Problem, result.Type);
    }
}
