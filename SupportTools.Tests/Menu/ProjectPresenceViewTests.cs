using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AppCliTools.CliMenu;
using SupportTools.Menu;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu;

public sealed class ProjectPresenceViewTests : IDisposable
{
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public void Dispose()
    {
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void CreatePart_WhenProjectIsLocal_ReturnsLocalInTheConsoleColor()
    {
        // Act
        StatusColorPart result = ProjectPresenceView.CreatePart(true);

        // Assert
        Assert.Equal("local", result.Text);
        Assert.Equal(Console.ForegroundColor, result.Color);
    }

    [Fact]
    public void CreatePart_WhenProjectIsRemote_ReturnsRemoteInDarkGray()
    {
        // Act
        StatusColorPart result = ProjectPresenceView.CreatePart(false);

        // Assert
        Assert.Equal("remote", result.Text);
        Assert.Equal(ConsoleColor.DarkGray, result.Color);
    }

    [Fact]
    public void CreateCountParts_WhenBothKindsExist_CountsLocalThenRemote()
    {
        // Act
        List<StatusColorPart> result =
            ProjectPresenceView.CreateCountParts([Local(), Remote(), Local(), Remote(), Remote()]);

        // Assert
        Assert.Equal(["local: 2", "remote: 3"], result.Select(x => x.Text));
        Assert.Equal([Console.ForegroundColor, ConsoleColor.DarkGray], result.Select(x => x.Color));
    }

    [Fact]
    public void CreateCountParts_WhenOnlyLocalProjects_LeavesOutTheZeroRemoteCount()
    {
        // Act
        List<StatusColorPart> result = ProjectPresenceView.CreateCountParts([Local()]);

        // Assert
        Assert.Equal(["local: 1"], result.Select(x => x.Text));
    }

    [Fact]
    public void CreateCountParts_WhenOnlyRemoteProjects_LeavesOutTheZeroLocalCount()
    {
        // Act
        List<StatusColorPart> result = ProjectPresenceView.CreateCountParts([Remote()]);

        // Assert
        Assert.Equal(["remote: 1"], result.Select(x => x.Text));
    }

    [Fact]
    public void CreateCountParts_WhenThereAreNoProjects_ReturnsNoParts()
    {
        // Act
        List<StatusColorPart> result = ProjectPresenceView.CreateCountParts([]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetText_WhenCalled_JoinsThePartTextsLikeTheMenu()
    {
        // Act
        string result = ProjectPresenceView.GetText([
            new StatusColorPart("local: 1", ConsoleColor.Gray), new StatusColorPart("errors: 2", ConsoleColor.Red)
        ]);

        // Assert
        Assert.Equal("local: 1, errors: 2", result);
    }

    [Theory]
    [InlineData(false, new[] { "LocalA", "RemoteA" })]
    [InlineData(true, new[] { "LocalA" })]
    public void GetVisibleProjects_WhenRemoteOnlyProjectsAreHiddenOrNot_FiltersOnlyWhenHidden(bool hide,
        string[] expected)
    {
        // Arrange
        var parameters = new SupportToolsParameters { HideRemoteOnlyProjects = hide };
        parameters.Projects["LocalA"] = Local();
        parameters.Projects["RemoteA"] = Remote();

        // Act
        List<string> result = [.. ProjectPresenceView.GetVisibleProjects(parameters).Select(x => x.Key)];

        // Assert
        Assert.Equal(expected, result);
    }

    private ProjectModel Local()
    {
        return new ProjectModel { ProjectFolderName = _tempFolder };
    }

    private ProjectModel Remote()
    {
        return new ProjectModel { ProjectFolderName = Path.Combine(_tempFolder, "Missing") };
    }
}
