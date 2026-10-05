using System.Linq;
using LibSupportToolsServerWork.Registry.Adapters;
using Xunit;

namespace SupportTools.Tests.Registry.Adapters;

public sealed class RegistrySyncWarningsTests
{
    private readonly RegistrySyncWarnings _sut = new();

    [Fact]
    public void Add_WhenWarningIsNew_StoresIt()
    {
        // Act
        _sut.Add("GitIgnorePatterns", "React", "file is missing");

        // Assert
        Assert.Equal([new RegistrySyncWarning("GitIgnorePatterns", "React", "file is missing")], _sut.Items);
    }

    //the engine reads the local records more than once in one sync
    [Fact]
    public void Add_WhenSameWarningComesAgain_KeepsOne()
    {
        // Arrange
        _sut.Add("GitIgnorePatterns", null, "folder is not set");

        // Act
        _sut.Add("GitIgnorePatterns", null, "folder is not set");

        // Assert
        Assert.Single(_sut.Items);
    }

    [Fact]
    public void Add_WhenKeyDiffers_KeepsBoth()
    {
        // Arrange
        _sut.Add("GitIgnorePatterns", "React", "file is missing");

        // Act
        _sut.Add("GitIgnorePatterns", "CSharp", "file is missing");

        // Assert
        Assert.Equal(["React", "CSharp"], _sut.Items.Select(x => x.Key));
    }
}
