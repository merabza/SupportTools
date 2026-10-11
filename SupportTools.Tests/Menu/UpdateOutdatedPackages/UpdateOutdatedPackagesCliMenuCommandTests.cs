using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.UpdateOutdatedPackages;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.UpdateOutdatedPackages;

//no git and no dotnet outdated run: WorkFolder is not set, Local1's git is a Node git (its update returns at once)
//and Remote1's git is not in Gits, so including Remote1 makes the command fail
[Collection(ConsoleCaptureCollection.Name)]
public sealed class UpdateOutdatedPackagesCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<string> _messageFieldNames = [];
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public UpdateOutdatedPackagesCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Gits["GitA"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/GitA.git", GitProjectFolderName = "GitA", GitIgnorePatternName = "Node"
        };
        _parameters.Projects["Local1"] = new ProjectModel { ProjectFolderName = _tempFolder, GitProjectNames = ["GitA"] };
        _parameters.Projects["Remote1"] = new ProjectModel
        {
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"), GitProjectNames = ["MissingGit"]
        };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_AsksBeforeRunningAndReloadsTheMenu()
    {
        // Act
        var sut = new UpdateOutdatedPackagesCliMenuCommand(new Mock<ILogger>().Object, _parametersManager.Object);

        // Assert
        Assert.Equal("Update Outdated Packages", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreNotIncluded_UpdatesOnlyTheLocalProjects()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([("Include 1 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        Assert.Equal(["Message"], _messageFieldNames);
        Assert.Contains("Update Outdated Packages Finished", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreIncluded_UpdatesThemToo()
    {
        // Arrange
        _answer = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("dotnet outdated failed for: MissingGit", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenEveryProjectIsLocal_DoesNotAsk()
    {
        // Arrange
        _parameters.Projects.Remove("Remote1");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
    }

    private UpdateOutdatedPackagesCliMenuCommand CreateSut()
    {
        return new UpdateOutdatedPackagesCliMenuCommand(new Mock<ILogger>().Object, _parametersManager.Object,
            (question, defaultValue) =>
            {
                _questions.Add((question, defaultValue));
                return _answer;
            }, (fieldName, _) =>
            {
                _messageFieldNames.Add(fieldName);
                return "Update message";
            });
    }
}
