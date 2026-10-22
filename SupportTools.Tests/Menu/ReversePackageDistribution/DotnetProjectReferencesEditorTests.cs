using System;
using System.Globalization;
using System.IO;
using LibDotnetWork;
using SupportTools.Menu.ReversePackageDistribution;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Menu.ReversePackageDistribution;

//runs the real dotnet on a project file that does not exist, so every command fails at once (no restore, no network)
//and nothing is changed; the result of dotnet comes back unchanged
[Collection(ConsoleCaptureCollection.Name)]
public sealed class DotnetProjectReferencesEditorTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);

    private readonly string _missingProject =
        Path.Combine(Path.GetTempPath(), $"SupportToolsTests_{Guid.NewGuid():N}", "Missing.csproj");

    private readonly TextWriter _originalConsoleOutput;
    private readonly DotnetProjectReferencesEditor _sut = new(new DotnetProcessor(null, true));

    public DotnetProjectReferencesEditorTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void RemovePackageFromProject_WhenProjectDoesNotExist_ReturnsTheFailureOfDotnet()
    {
        // Act
        Result result = _sut.RemovePackageFromProject(_missingProject, "Lib.A");

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddReferenceToProject_WhenProjectDoesNotExist_ReturnsTheFailureOfDotnet()
    {
        // Act
        Result result = _sut.AddReferenceToProject(_missingProject, _missingProject);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddPackageToProject_WhenProjectDoesNotExist_ReturnsTheFailureOfDotnet()
    {
        // Act
        Result result = _sut.AddPackageToProject(_missingProject, "Lib.A", null);

        // Assert
        Assert.True(result.IsFailure);
    }
}
