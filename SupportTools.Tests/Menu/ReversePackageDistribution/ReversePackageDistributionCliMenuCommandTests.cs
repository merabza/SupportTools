using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ReversePackageDistribution;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Menu.ReversePackageDistribution;

//no dotnet runs: the csproj changes go to a fake editor. Project App (folder {temp}\App) with its main git App;
//App\App\App.Api\App.Api.csproj uses the own packages Lib.A and Lib.B (GitProjects) and the foreign Newtonsoft.Json;
//the project file of Lib.A exists, that of Lib.B does not
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ReversePackageDistributionCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly string _csprojFile;
    private readonly FakeProjectReferencesEditor _editor = new();
    private readonly string _libAProjectFile;
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _projectFolder;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ReversePackageDistributionCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectFolder = Path.Combine(_tempFolder, "App");
        string apiFolder = Path.Combine(_projectFolder, "App", "App.Api");
        Directory.CreateDirectory(apiFolder);
        _csprojFile = Path.Combine(apiFolder, "App.Api.csproj");
        File.WriteAllText(_csprojFile, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Lib.A" />
                <PackageReference Include="Newtonsoft.Json" />
                <PackageReference Include="Lib.B" />
              </ItemGroup>
            </Project>
            """);
        string libAFolder = Path.Combine(_projectFolder, "Lib", "Lib.A");
        Directory.CreateDirectory(libAFolder);
        _libAProjectFile = Path.Combine(libAFolder, "Lib.A.csproj");
        File.WriteAllText(_libAProjectFile, "<Project />");

        _parameters.Projects["App"] = new ProjectModel
        {
            UseLocalPackages = true, ProjectFolderName = _projectFolder, GitProjectNames = ["App"]
        };
        _parameters.Gits["App"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/App.git", GitProjectFolderName = "App", GitIgnorePatternName = "CSharp"
        };
        _parameters.GitProjects["Lib.A"] = new GitProjectDataModel
        {
            GitName = "Lib", ProjectRelativePath = @"Lib\Lib.A", ProjectFileName = "Lib.A.csproj"
        };
        _parameters.GitProjects["Lib.B"] = new GitProjectDataModel
        {
            GitName = "Lib", ProjectRelativePath = @"Lib\Lib.B", ProjectFileName = "Lib.B.csproj"
        };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
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
        var sut = new ReversePackageDistributionCliMenuCommand(_logger.Object, _parametersManager.Object);

        // Assert
        Assert.Equal("Reverse Package Distribution", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task GetActionDescription_WhenCalled_DescribesTheChange()
    {
        // Act
        string? result = await CliMenuTestAccess.InvokeGetActionDescription(CreateSut());

        // Assert
        Assert.Equal(
            "This process will replace PackageReferences of own packages with ProjectReferences in main repositories of all projects",
            result);
    }

    //only own packages whose project file exists are replaced: the package goes, the project reference comes
    [Fact]
    public async Task RunBody_WhenOwnPackageProjectExists_ReplacesThePackageWithAProjectReference()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([$"remove {_csprojFile} Lib.A", $"reference {_csprojFile} {_libAProjectFile}"], _editor.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains($"Processing {_csprojFile}", output, StringComparison.Ordinal);
        Assert.Contains(
            $"Project file {Path.Combine(_projectFolder, "Lib", "Lib.B", "Lib.B.csproj")} does not exists for package Lib.B, package reference in {_csprojFile} is not replaced",
            output, StringComparison.Ordinal);
        VerifyLogged(LogLevel.Information,
            $"Replaced package Lib.A with ProjectReference {_libAProjectFile} in {_csprojFile}");
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 1");
    }

    [Fact]
    public async Task RunBody_WhenPackageCannotBeRemoved_ReportsItAndFails()
    {
        // Arrange
        _editor.FailingRemove = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal([$"remove {_csprojFile} Lib.A"], _editor.Calls);
        Assert.Contains($"Cannot remove package Lib.A from {_csprojFile}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //a reference that cannot be added gives the removed package back, so the project still builds
    [Fact]
    public async Task RunBody_WhenReferenceCannotBeAdded_RestoresThePackageAndFails()
    {
        // Arrange
        _editor.FailingReference = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(
        [
            $"remove {_csprojFile} Lib.A", $"reference {_csprojFile} {_libAProjectFile}",
            $"package {_csprojFile} Lib.A "
        ], _editor.Calls);
        Assert.Contains($"Cannot add reference {_libAProjectFile} to {_csprojFile}. Removed package was restored",
            _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifyNotLogged("Replaced package");
    }

    //every failing project counts: two failures are still a failure
    [Fact]
    public async Task RunBody_WhenTwoProjectsFail_Fails()
    {
        // Arrange
        AddProjectWithOwnPackage("Another");
        _editor.FailingRemove = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(2, _editor.Calls.Count);
    }

    //the projects go in name order
    [Fact]
    public async Task RunBody_WhenThereAreSeveralProjects_ProcessesThemInNameOrder()
    {
        // Arrange
        string anotherCsproj = AddProjectWithOwnPackage("Another");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        string output = _consoleOutput.ToString();
        Assert.True(output.IndexOf($"Processing {anotherCsproj}", StringComparison.Ordinal) <
                    output.IndexOf($"Processing {_csprojFile}", StringComparison.Ordinal));
    }

    //only csproj files are read; the gits are read with the console, so a git without an address is reported
    [Fact]
    public async Task RunBody_WhenRepositoryHasOtherFilesAndAGitHasNoAddress_ReadsOnlyTheProjectFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_projectFolder, "App", "Readme.txt"), "not xml");
        _parameters.Gits["NoAddress"] = new GitDataModel { GitProjectFolderName = "NoAddress", GitIgnorePatternName = "CSharp" };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(2, _editor.Calls.Count);
        Assert.Contains("GitProjectAddress is empty for Git Repo with key NoAddress)", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRemovedPackageCannotBeRestored_AsksToRecoverTheFileFromGit()
    {
        // Arrange
        _editor.FailingReference = true;
        _editor.FailingPackage = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        Assert.Contains(
            $"Cannot add reference {_libAProjectFile} to {_csprojFile} and cannot restore removed package Lib.A. Recover the file from git",
            output, StringComparison.Ordinal);
        Assert.DoesNotContain("Removed package was restored", output, StringComparison.Ordinal);
        VerifyNotLogged("Replaced package");
    }

    [Fact]
    public async Task RunBody_WhenProjectDoesNotUseLocalPackages_SkipsIt()
    {
        // Arrange
        _parameters.Projects["App"].UseLocalPackages = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        Assert.Contains("UseLocalPackages is off for project App, skipped", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 0");
    }

    [Fact]
    public async Task RunBody_WhenProjectIsRemote_SkipsIt()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel
        {
            UseLocalPackages = true,
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"), GitProjectNames = ["App"]
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        Assert.Contains("1 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 0");
    }

    //the main repository is the git named like the project
    [Fact]
    public async Task RunBody_WhenProjectHasNoMainGit_SkipsIt()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel
        {
            UseLocalPackages = true, ProjectFolderName = _projectFolder, GitProjectNames = ["Lib"]
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        Assert.Contains("Main git repository does not found for project App, skipped", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenMainGitIsNotInGits_SkipsTheProject()
    {
        // Arrange
        _parameters.Gits.Clear();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        Assert.Contains("Main git repository does not found for project App, skipped", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenMainRepositoryFolderDoesNotExist_SkipsTheProject()
    {
        // Arrange
        _parameters.Gits["App"].GitProjectFolderName = "MissingFolder";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        Assert.Contains(
            $"Main repository folder {Path.Combine(_projectFolder, "MissingFolder")} does not exists for project App, skipped",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenCancelled_Throws()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        Exception exception = await Record.ExceptionAsync(() =>
            CliMenuTestAccess.InvokeRunBody(CreateSut(), cancellationTokenSource.Token));

        // Assert
        Assert.IsType<OperationCanceledException>(exception.InnerException ?? exception);
        Assert.Empty(_editor.Calls);
    }

    private ReversePackageDistributionCliMenuCommand CreateSut()
    {
        return new ReversePackageDistributionCliMenuCommand(_logger.Object, _parametersManager.Object, () => _editor);
    }

    //another local project like App: {temp}\{name} with its main git {name} and {name}\{name}.Api\{name}.Api.csproj,
    //which uses Lib.A; returns that csproj
    private string AddProjectWithOwnPackage(string projectName)
    {
        string projectFolder = Path.Combine(_tempFolder, projectName);
        string apiFolder = Path.Combine(projectFolder, projectName, $"{projectName}.Api");
        Directory.CreateDirectory(apiFolder);
        string csprojFile = Path.Combine(apiFolder, $"{projectName}.Api.csproj");
        File.WriteAllText(csprojFile,
            """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Lib.A" /></ItemGroup></Project>""");
        string libAFolder = Path.Combine(projectFolder, "Lib", "Lib.A");
        Directory.CreateDirectory(libAFolder);
        File.WriteAllText(Path.Combine(libAFolder, "Lib.A.csproj"), "<Project />");
        _parameters.Projects[projectName] = new ProjectModel
        {
            UseLocalPackages = true, ProjectFolderName = projectFolder, GitProjectNames = [projectName]
        };
        _parameters.Gits[projectName] = new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{projectName}.git",
            GitProjectFolderName = projectName,
            GitIgnorePatternName = "CSharp"
        };
        return csprojFile;
    }

    private void VerifyNotLogged(string messageStart)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) => value.ToString()!.StartsWith(messageStart, StringComparison.Ordinal)),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
#pragma warning restore CA1873
    }

    private void VerifyLogged(LogLevel logLevel, string message)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(x => x.Log(logLevel, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) => value.ToString() == message), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }

    private sealed class FakeProjectReferencesEditor : IProjectReferencesEditor
    {
        public List<string> Calls { get; } = [];
        public bool FailingRemove { get; set; }
        public bool FailingReference { get; set; }
        public bool FailingPackage { get; set; }

        public Result RemovePackageFromProject(string projectFilePath, string packageName)
        {
            Calls.Add($"remove {projectFilePath} {packageName}");
            return CreateResult(FailingRemove);
        }

        public Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
        {
            Calls.Add($"reference {projectFilePath} {referenceProjectFilePath}");
            return CreateResult(FailingReference);
        }

        public Result AddPackageToProject(string projectFilePath, string packageName, string? packageVersion)
        {
            Calls.Add($"package {projectFilePath} {packageName} {packageVersion}");
            return CreateResult(FailingPackage);
        }

        private static Result CreateResult(bool failing)
        {
            return failing ? Result.Failure(Error.Failure("Failed", "failed")) : Result.Success();
        }
    }
}
