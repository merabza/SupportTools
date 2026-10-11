using System;
using System.Globalization;
using System.IO;
using LibGitWork.ToolCommandParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolCommandParameters;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncMultipleProjectsGitsParametersV2Tests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

    public SyncMultipleProjectsGitsParametersV2Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_WhenCalled_KeepsTheProjectsTheSelectionAndTheRemoteChoice(bool includeRemoteProjects)
    {
        // Arrange
        var parameters = new SupportToolsParameters { ScaffoldSeedersWorkFolder = @"D:\Seeders" };

        // Act
        var result = SyncMultipleProjectsGitsParametersV2.Create(parameters, "Group", "Project",
            includeRemoteProjects);

        // Assert
        Assert.Equal(@"D:\Seeders", result.ScaffoldSeedersWorkFolder);
        Assert.Same(parameters.Projects, result.Projects);
        Assert.Equal("Group", result.ProjectGroupName);
        Assert.Equal("Project", result.ProjectName);
        Assert.Equal(includeRemoteProjects, result.IncludeRemoteProjects);
        Assert.True(result.CheckBeforeSave());
        Assert.Empty(_consoleOutput.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_WhenScaffoldSeedersWorkFolderIsNotSpecified_WarnsAboutIt(string? scaffoldSeedersWorkFolder)
    {
        // Arrange
        var parameters = new SupportToolsParameters { ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder };

        // Act
        var result = SyncMultipleProjectsGitsParametersV2.Create(parameters, null, null, true);

        // Assert
        Assert.Null(result.ProjectGroupName);
        Assert.Contains("ScaffoldSeedersWorkFolder is not specified", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }
}
