using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.UpdateOutdatedPackages;
using SupportTools.Tests.OutdatedPackages;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.UpdateOutdatedPackages;

//no git and no dotnet outdated run: the steps are FakeOutdatedPackagesUpdateSteps. Local1 [GitA] exists here,
//Remote1 [MissingGit] does not
[Collection(ConsoleCaptureCollection.Name)]
public sealed class UpdateOutdatedPackagesCliMenuCommandTests : IDisposable
{
    private const string RemoteQuestion = "Include 1 remote-only projects (their gits will be cloned to this computer)?";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<string> _messageFieldNames = [];
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly FakeOutdatedPackagesUpdateSteps _steps = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public UpdateOutdatedPackagesCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Gits["GitA"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/GitA.git",
            GitProjectFolderName = "GitA",
            GitIgnorePatternName = "CSharp"
        };
        _parameters.Projects["Local1"] =
            new ProjectModel { ProjectFolderName = _tempFolder, GitProjectNames = ["GitA"] };
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
        var sut = new UpdateOutdatedPackagesCliMenuCommand(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object);

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
        Assert.Equal([(RemoteQuestion, false)], _questions);
        Assert.Equal(["Message"], _messageFieldNames);
        Assert.Equal(
        [
            "sync all: Update message Check", "sync Local1: Update message Prepare", "update Local1/GitA",
            "sync Local1: Update message Update"
        ], _steps.Steps);
        Assert.All(_steps.IncludeRemoteProjectsValues, Assert.False);
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
        Assert.True(result);
        Assert.Contains("update Remote1/MissingGit", _steps.Steps);
        Assert.All(_steps.IncludeRemoteProjectsValues, Assert.True);
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

    //the question whether to go on after a failed sync of all gits is asked through the same input as the first one
    [Fact]
    public async Task RunBody_WhenTheCheckSyncFails_AsksWhetherToContinue()
    {
        // Arrange
        _parameters.Projects.Remove("Remote1");
        _steps.FailingSteps.Add("sync all: Update message Check");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal([("Some gits were not synced (see above). Continue updating packages?", false)], _questions);
    }

    private UpdateOutdatedPackagesCliMenuCommand CreateSut()
    {
        return new UpdateOutdatedPackagesCliMenuCommand(new Mock<ILogger>().Object, _parametersManager.Object, _steps,
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
