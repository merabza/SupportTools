using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGitData.Models;
using LibGitWork.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.GitProjects;

//D3: a project with SpaProjectName whose GitProjects record is missing stopped the V2 sync with the exception of
//GitProjects.GetGitProjectByKey; the helper reports it instead.
//Project Front: git FrontBack (plain folder) and git FrontSpa ({SpaProjectFolderRelativePath}\ClientApp)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SpaProjectFolderHelperTests : IDisposable
{
    internal const string MissingMessage =
        "GitProjects has no FrontApp (SpaProjectName of project Front): run \"Update Git Projects\" or \"Update Git Projects From SupportToolsServer\"";

    private const string ProjectName = "Front";
    private const string BackGitName = "FrontBack";
    private const string SpaGitName = "FrontSpa";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Dictionary<string, GitDataModel> _gits = new()
    {
        [BackGitName] = new GitDataModel { GitProjectFolderName = BackGitName },
        [SpaGitName] = new GitDataModel
        {
            GitProjectFolderName = $@"{GitDataModel.SpaProjectFolderRelativePathName}\ClientApp"
        }
    };

    private readonly Dictionary<string, GitProjectDataModel> _gitProjects = [];
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly ProjectModel _project = new() { SpaProjectName = "FrontApp" };

    public SpaProjectFolderHelperTests()
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
    public void GetMissingSpaProjectMessage_WhenCalled_NamesTheRecordTheProjectAndTheCommands()
    {
        // Act
        string result = SpaProjectFolderHelper.GetMissingSpaProjectMessage(ProjectName, "FrontApp");

        // Assert
        Assert.Equal(MissingMessage, result);
    }

    [Fact]
    public void GetSpaProjectFolderRelativePath_WhenRecordIsMissingAndGitNeedsIt_ReportsMissingWithoutException()
    {
        // Act
        (string? path, bool isMissing) = GetSpaProjectFolderRelativePath([BackGitName, SpaGitName], true);

        // Assert
        Assert.Null(path);
        Assert.True(isMissing);
        Assert.Contains(MissingMessage, _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifyErrorLogged(Times.Once());
    }

    [Fact]
    public void GetSpaProjectFolderRelativePath_WhenConsoleIsNotUsed_OnlyLogsTheError()
    {
        // Act
        (_, bool isMissing) = GetSpaProjectFolderRelativePath([SpaGitName], false);

        // Assert
        Assert.True(isMissing);
        Assert.Empty(_consoleOutput.ToString());
        VerifyErrorLogged(Times.Once());
    }

    [Fact]
    public void GetSpaProjectFolderRelativePath_WhenRecordIsMissingButNoGitNeedsIt_ReportsNothing()
    {
        // Act
        (string? path, bool isMissing) = GetSpaProjectFolderRelativePath([BackGitName, "UnknownGit"], true);

        // Assert
        Assert.Null(path);
        Assert.False(isMissing);
        Assert.Empty(_consoleOutput.ToString());
        VerifyErrorLogged(Times.Never());
    }

    [Fact]
    public void GetSpaProjectFolderRelativePath_WhenRecordExists_ReturnsItsRelativePath()
    {
        // Arrange
        _gitProjects["FrontApp"] = new GitProjectDataModel
        {
            GitName = BackGitName, ProjectRelativePath = @"FrontBack\FrontApp", ProjectFileName = "FrontApp.esproj"
        };

        // Act
        (string? path, bool isMissing) = GetSpaProjectFolderRelativePath([SpaGitName], true);

        // Assert
        Assert.Equal(@"FrontBack\FrontApp", path);
        Assert.False(isMissing);
    }

    [Fact]
    public void GetSpaProjectFolderRelativePath_WhenProjectHasNoSpaProject_ReturnsNullWithoutError()
    {
        // Arrange
        _project.SpaProjectName = null;

        // Act
        (string? path, bool isMissing) = GetSpaProjectFolderRelativePath([SpaGitName], true);

        // Assert
        Assert.Null(path);
        Assert.False(isMissing);
        Assert.Empty(_consoleOutput.ToString());
    }

    [Theory]
    [InlineData(@"{SpaProjectFolderRelativePath}\ClientApp", true)]
    [InlineData("{SpaProjectFolderRelativePath}", true)]
    [InlineData(@"Front\{SpaProjectFolderRelativePath}", false)]
    [InlineData("{spaprojectfolderrelativepath}", false)]
    [InlineData("FrontBack", false)]
    [InlineData(null, false)]
    public void UsesSpaProjectFolder_WhenCalled_TellsWhetherTheFolderStartsWithThePlaceholder(string? folderName,
        bool expected)
    {
        // Act
        bool result = SpaProjectFolderHelper.UsesSpaProjectFolder(new GitDataModel { GitProjectFolderName = folderName });

        // Assert
        Assert.Equal(expected, result);
    }

    private (string? Path, bool IsMissing) GetSpaProjectFolderRelativePath(List<string> gitNames, bool useConsole)
    {
        return SpaProjectFolderHelper.GetSpaProjectFolderRelativePath(_logger.Object, ProjectName, _project,
            LibGitData.Models.GitProjects.Create(null, _gitProjects), _gits, gitNames, useConsole);
    }

    private void VerifyErrorLogged(Times times)
    {
        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) => value.ToString() == MissingMessage), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
    }
}
