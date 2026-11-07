using System;
using System.Globalization;
using System.IO;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.ToolActions;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests;

//GitR is a C# git of the projects P1 and P2 that is not cloned, so a real dotnet outdated on its folder fails at once
[Collection(ConsoleCaptureCollection.Name)]
public sealed class PackageUpdaterTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public PackageUpdaterTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Gits["GitR"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/GitR.git",
            GitProjectFolderName = "GitR",
            GitIgnorePatternName = PackageUpdaterToolAction.CSharp
        };
        AddProject("P1");
        AddProject("P2");
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Add_WhenTheProjectDoesNotListTheGit_AddsNothing()
    {
        // Arrange
        var sut = new PackageUpdater(null, _parametersManager.Object, "GitX", true);

        // Act
        sut.Add("P1");

        // Assert
        Assert.Equal(0, sut.Count);
        Assert.Contains("GitSyncParameters is not created", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Add_WhenTheProjectListsTheGit_AddsItsUpdate()
    {
        // Arrange
        var sut = new PackageUpdater(null, _parametersManager.Object, "GitR", true);

        // Act
        sut.Add("P1");

        // Assert
        Assert.Equal(1, sut.Count);
    }

    [Fact]
    public void Run_WhenNothingWasAdded_RunsNothing()
    {
        // Arrange
        var sut = new PackageUpdater(null, _parametersManager.Object, "GitR", true);

        // Act
        sut.Run();

        // Assert
        Assert.Empty(_consoleOutput.ToString());
    }

    //only the update in the folder of the first project runs
    [Fact]
    public void Run_WhenUpdatesWereAdded_RunsTheFirstOne()
    {
        // Arrange
        var sut = new PackageUpdater(null, _parametersManager.Object, "GitR", true);
        sut.Add("P1");
        sut.Add("P2");

        // Act
        sut.Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains($"GitR: dotnet outdated failed for {Path.Combine(_tempFolder, "P1", "GitR")} (attempt 1 of 3)",
            output, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.Combine(_tempFolder, "P2"), output, StringComparison.Ordinal);
    }

    private void AddProject(string projectName)
    {
        string projectFolder = Path.Combine(_tempFolder, projectName);
        Directory.CreateDirectory(projectFolder);
        _parameters.Projects[projectName] =
            new ProjectModel { ProjectFolderName = projectFolder, GitProjectNames = ["GitR"] };
    }
}
