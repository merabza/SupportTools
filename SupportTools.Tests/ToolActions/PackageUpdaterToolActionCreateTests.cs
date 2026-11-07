using System;
using System.Globalization;
using System.IO;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Tests.OutdatedPackages;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//the project PR lists GitR; the git is not cloned, so a real dotnet outdated on its folder fails at once
[Collection(ConsoleCaptureCollection.Name)]
public sealed class PackageUpdaterToolActionCreateTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _projectFolder;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public PackageUpdaterToolActionCreateTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectFolder = Path.Combine(_tempFolder, "PR");
        Directory.CreateDirectory(_projectFolder);
        _parameters.Projects["PR"] =
            new ProjectModel { ProjectFolderName = _projectFolder, GitProjectNames = ["GitR"] };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Create_WhenTheGitIsNotInGits_WritesWhyAndReturnsNull()
    {
        // Act
        PackageUpdaterToolAction? result =
            PackageUpdaterToolAction.Create(null, _parametersManager.Object, "PR", "GitR", true);

        // Assert
        Assert.Null(result);
        Assert.Contains("GitSyncParameters is not created", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the update works in the folder of the git inside the project folder
    [Fact]
    public void Create_WhenTheGitIsACSharpGit_UpdatesTheFolderOfTheGitInTheProjectFolder()
    {
        // Arrange
        AddGit(PackageUpdaterToolAction.CSharp);
        var output = new FakePackageUpdateOutput();

        // Act
        EPackageUpdateResult result = PackageUpdaterToolAction.Create(null, _parametersManager.Object, "PR", "GitR",
            true)!.RunPackageUpdate(output);

        // Assert
        Assert.Equal(EPackageUpdateResult.OutdatedFailed, result);
        Assert.StartsWith($"dotnet outdated failed for {Path.Combine(_projectFolder, "GitR")} (attempt 1 of 3)",
            output.Lines[0].Text, StringComparison.Ordinal);
    }

    //the type of the git decides: the update of another type runs nothing
    [Fact]
    public void Create_WhenTheGitIsOfAnotherType_CreatesAnUpdateThatRunsNothing()
    {
        // Arrange
        AddGit("Node");
        var output = new FakePackageUpdateOutput();

        // Act
        EPackageUpdateResult result = PackageUpdaterToolAction.Create(null, _parametersManager.Object, "PR", "GitR",
            true)!.RunPackageUpdate(output);

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Empty(output.Statuses);
    }

    //GitSyncParameters writes why the git is not in Gits with the logger it gets
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_WhenGitWorkIsLoggedOrNot_GivesTheLoggerToTheGitParametersOnlyWhenLogged(bool logGitWork)
    {
        // Arrange
        _parameters.LogGitWork = logGitWork;
        var logger = new Mock<ILogger>();

        // Act
        PackageUpdaterToolAction.Create(logger.Object, _parametersManager.Object, "PR", "GitR", true);

        // Assert
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value!.ToString() == "Git Project with name GitR does not exists in project PR"),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            logGitWork ? Times.Once() : Times.Never());
#pragma warning restore CA1873
    }

    private void AddGit(string gitIgnorePatternName)
    {
        _parameters.Gits["GitR"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/GitR.git",
            GitProjectFolderName = "GitR",
            GitIgnorePatternName = gitIgnorePatternName
        };
    }
}
