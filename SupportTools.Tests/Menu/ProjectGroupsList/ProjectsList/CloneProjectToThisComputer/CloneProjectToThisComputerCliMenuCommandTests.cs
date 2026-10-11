using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//D3: "Clone Project To This Computer" over fake git steps. Project App (folder {temp}\Projects\App, missing):
//gits AppMain (plain folder) and AppSpa ({SpaProjectFolderRelativePath}\ClientApp, SpaProjectName AppFront is a project
//of AppMain), ScaffoldSeeder git AppSeeder; the .gitignore template CSharp is "bin/"
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CloneProjectToThisComputerCliMenuCommandTests : IDisposable
{
    private const string ProjectName = "App";
    private const string MainGitName = "AppMain";
    private const string SpaGitName = "AppSpa";
    private const string SeederGitName = "AppSeeder";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly FakeProjectCloneGitActions _gitActions;
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly ProjectModel _project;
    private readonly string _projectFolder;
    private readonly List<string> _questions = [];
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), $"CloneProjectTests_{Guid.NewGuid():N}");
    private bool _confirmAnswer = true;
    private bool _scaffoldSeederAnswer;
    private Func<CancellationToken, ValueTask> _syncRegistry = _ => ValueTask.CompletedTask;
    private bool _syncRegistryAnswer = true;
    private int _syncRegistryRuns;

    public CloneProjectToThisComputerCliMenuCommandTests()
    {
        Directory.CreateDirectory(_tempFolder);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectFolder = Path.Combine(_tempFolder, "Projects", ProjectName);
        _project = new ProjectModel
        {
            ProjectFolderName = _projectFolder,
            SpaProjectName = "AppFront",
            ScaffoldSeederProjectName = ProjectName,
            GitProjectNames = [MainGitName, SpaGitName],
            ScaffoldSeederGitProjectNames = [SeederGitName]
        };
        _parameters.Projects[ProjectName] = _project;
        _parameters.ScaffoldSeedersWorkFolder = Path.Combine(_tempFolder, "Seeders");
        _parameters.FolderForGitignoreFiles = Path.Combine(_tempFolder, "Templates");
        Directory.CreateDirectory(_parameters.FolderForGitignoreFiles);
        File.WriteAllText(Path.Combine(_parameters.FolderForGitignoreFiles, "CSharp.gitignore"), "bin/");
        _parameters.Gits[MainGitName] = CreateGit(MainGitName, MainGitName);
        _parameters.Gits[SpaGitName] =
            CreateGit(SpaGitName, $@"{GitDataModel.SpaProjectFolderRelativePathName}\ClientApp");
        _parameters.Gits[SeederGitName] = CreateGit(SeederGitName, SeederGitName);

        _gitActions = new FakeProjectCloneGitActions(_parameters);
        _gitActions.ScanResults[MainGitName] = new Dictionary<string, GitProjectDataModel>
        {
            ["AppFront"] = CreateGitProject(MainGitName, @"AppMain\AppFront", "AppFront.esproj"),
            ["AppApi"] = CreateGitProject(MainGitName, @"AppMain\AppApi", "AppApi.csproj")
        };

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public async Task Run_WhenProjectIsRemote_CreatesFolderScansBeforeCloningAndReportsSuccess()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(_projectFolder));
        Assert.DoesNotContain(_questions, x => x.Contains("files of the project are missing", StringComparison.Ordinal));
        Assert.Equal(["scan AppMain", "scan AppSpa", "sync Main AppMain", "sync Main AppSpa"], _gitActions.Calls);
        Assert.True(Directory.Exists(Path.Combine(_projectFolder, MainGitName)));
        Assert.True(Directory.Exists(Path.Combine(_projectFolder, @"AppMain\AppFront\ClientApp")));
        _parametersManager.Verify(x => x.Save(_parameters, "Git Projects Updated", null,
            It.IsAny<CancellationToken>()), Times.Once);
        string output = _consoleOutput.ToString();
        //the report is the only output (the fake git steps write nothing) and starts with an empty line
        Assert.StartsWith($"{Environment.NewLine}Clone Project To This Computer App: finished", output,
            StringComparison.Ordinal);
        Assert.Contains($"  Folder {_projectFolder} created", output, StringComparison.Ordinal);
        Assert.Contains("GitProjects updated for gits: AppMain, AppSpa", output, StringComparison.Ordinal);
        Assert.Contains("Gits cloned: 2 of 2", output, StringComparison.Ordinal);
        Assert.Contains("Templates: OK", output, StringComparison.Ordinal);
        Assert.Contains("Project files: OK", output, StringComparison.Ordinal);
        Assert.DoesNotContain("finished with errors", output, StringComparison.Ordinal);
    }

    //the gits whose projects are already in GitProjects are not scanned and nothing is saved
    [Fact]
    public async Task Run_WhenGitProjectsHaveTheRecords_DoesNotScan()
    {
        // Arrange
        _parameters.GitProjects["AppFront"] = CreateGitProject(MainGitName, @"AppMain\AppFront", "AppFront.esproj");
        _parameters.GitProjects["AppClient"] = CreateGitProject(SpaGitName, @"AppSpa\AppClient", "AppClient.esproj");

        // Act
        await CreateSut().Run();

        // Assert
        Assert.Equal(["sync Main AppMain", "sync Main AppSpa"], _gitActions.Calls);
        _parametersManager.Verify(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    //without the SpaProjectName record the SPA git has no folder: a clear error instead of the old exception
    [Fact]
    public async Task Run_WhenScanDoesNotFindSpaProject_ReportsMissingRecordAndCloneFailure()
    {
        // Arrange
        _gitActions.ScanResults.Clear();

        // Act
        await CreateSut().Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("GitProjects has no AppFront (SpaProjectName of project App)", output, StringComparison.Ordinal);
        Assert.Contains("Gits cloned: 1 of 2", output, StringComparison.Ordinal);
        Assert.Contains("Gits not cloned: AppSpa", output, StringComparison.Ordinal);
        Assert.Contains("Clone Project To This Computer App: finished with errors", output, StringComparison.Ordinal);
    }

    //a SpaProjectName that no scan finds is an error even when no git of the project needs its folder
    [Fact]
    public async Task Run_WhenSpaProjectIsMissingButNoGitNeedsIt_FinishesWithErrors()
    {
        // Arrange
        _project.GitProjectNames.Remove(SpaGitName);
        _gitActions.ScanResults.Clear();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        Assert.Contains("Gits cloned: 1 of 1", output, StringComparison.Ordinal);
        Assert.Contains("GitProjects has no AppFront (SpaProjectName of project App)", output, StringComparison.Ordinal);
        Assert.Contains("Clone Project To This Computer App: finished with errors", output, StringComparison.Ordinal);
    }

    //nothing scanned, nothing saved
    [Fact]
    public async Task Run_WhenEveryScanFails_SavesNothing()
    {
        // Arrange
        _gitActions.FailingScans.Add(MainGitName);
        _gitActions.FailingScans.Add(SpaGitName);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        _parametersManager.Verify(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.DoesNotContain("GitProjects updated for gits", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenScanFails_ReportsTheGit()
    {
        // Arrange
        _gitActions.FailingScans.Add(SpaGitName);

        // Act
        await CreateSut().Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Update Git Projects failed for git AppSpa", output, StringComparison.Ordinal);
        Assert.Contains("GitProjects updated for gits: AppMain", output, StringComparison.Ordinal);
        Assert.Contains("finished with errors", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenScaffoldSeederGitsAreChosen_ClonesThemIntoTheSeederFolder()
    {
        // Arrange
        _scaffoldSeederAnswer = true;

        // Act
        await CreateSut().Run();

        // Assert
        Assert.Equal(
        [
            "scan AppMain", "scan AppSpa", "scan AppSeeder", "sync Main AppMain", "sync Main AppSpa",
            "sync ScaffoldSeed AppSeeder"
        ], _gitActions.Calls);
        Assert.True(Directory.Exists(Path.Combine(_tempFolder, "Seeders", "App", "AppScaffoldSeeder", SeederGitName)));
        Assert.Contains("Gits cloned: 3 of 3", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenScaffoldSeederGitsAreNotChosen_AsksWithDefaultNoAndSkipsThem()
    {
        // Act
        await CreateSut().Run();

        // Assert
        Assert.Contains("Clone also 1 ScaffoldSeeder gits of project App?|False", _questions);
        Assert.DoesNotContain("sync ScaffoldSeed AppSeeder", _gitActions.Calls);
    }

    [Fact]
    public async Task Run_WhenProjectHasNoScaffoldSeederGits_DoesNotAskAboutThem()
    {
        // Arrange
        _project.ScaffoldSeederGitProjectNames = [];

        // Act
        await CreateSut().Run();

        // Assert
        Assert.DoesNotContain(_questions, x => x.StartsWith("Clone also", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Run_WhenUserDoesNotConfirm_ChangesNothing()
    {
        // Arrange
        _confirmAnswer = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains($"Clone 2 gits of project App to {_projectFolder}?|True", _questions);
        Assert.False(Directory.Exists(_projectFolder));
        Assert.Empty(_gitActions.Calls);
    }

    [Fact]
    public async Task Run_WhenProjectIsAlreadyLocal_DoesNothing()
    {
        // Arrange
        Directory.CreateDirectory(_projectFolder);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
        Assert.Empty(_gitActions.Calls);
        Assert.Contains($"Project App is already on this computer: {_projectFolder}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Run_WhenProjectFolderNameIsEmpty_ReportsErrorAndDoesNothing(string? projectFolderName)
    {
        // Arrange
        _parameters.Projects[ProjectName] = new ProjectModel
        {
            ProjectFolderName = projectFolderName, GitProjectNames = [MainGitName]
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_gitActions.Calls);
        Assert.Contains("ProjectFolderName is not specified for project App", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenProjectDoesNotExist_ReportsError()
    {
        // Arrange
        _parameters.Projects.Clear();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_gitActions.Calls);
        Assert.Contains("Project App does not exist", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenGitIsNotCloned_ReportsIt()
    {
        // Arrange
        _gitActions.FailingClones.Add(SpaGitName);

        // Act
        await CreateSut().Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Gits cloned: 1 of 2", output, StringComparison.Ordinal);
        Assert.Contains("Gits not cloned: AppSpa", output, StringComparison.Ordinal);
        Assert.Contains("finished with errors", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenNoGitIsCloned_ListsThemAll()
    {
        // Arrange
        _gitActions.FailingClones.Add(MainGitName);
        _gitActions.FailingClones.Add(SpaGitName);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        Assert.Contains("Gits cloned: 0 of 2", output, StringComparison.Ordinal);
        Assert.Contains("Gits not cloned: AppMain, AppSpa", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenSeveralStoredFilesStayMissing_ListsThemAll()
    {
        // Arrange
        string seedParameters = Path.Combine(_tempFolder, "Security", "seed.json");
        string pairedObjects = Path.Combine(_tempFolder, "Security", "paired.json");
        _project.SeedProjectParametersFilePath = seedParameters;
        _project.PairedDbObjectsResultFileName = pairedObjects;
        _syncRegistryAnswer = false;

        // Act
        await CreateSut().Run();

        // Assert
        Assert.Contains("2 files of the project are missing. Run \"Sync Registry With SupportToolsServer...\" now?|True",
            _questions);
        Assert.Contains(
            $"Project files missing (run \"Sync Registry With SupportToolsServer...\"): {seedParameters}, {pairedObjects}",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenClonedGitignoreDiffersFromTemplate_ReportsTheTemplateProblem()
    {
        // Arrange
        _gitActions.GitIgnoreContent = "obj/";

        // Act
        await CreateSut().Run();

        // Assert
        Assert.Contains($"{Path.Combine(_projectFolder, MainGitName, ".gitignore")} differs from the .gitignore template CSharp",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //(ე) a missing secret file offers "Sync Registry", which brings it
    [Fact]
    public async Task Run_WhenStoredFileIsMissingAndSyncRegistryBringsIt_ReportsFilesOk()
    {
        // Arrange
        string appSettings = Path.Combine(_tempFolder, "Security", "appsettings.json");
        _project.ServerInfos["Server|Prod"] = new ServerInfoModel { AppSettingsJsonSourceFileName = appSettings };
        _syncRegistry = async cancellationToken =>
        {
            _syncRegistryRuns++;
            Directory.CreateDirectory(Path.GetDirectoryName(appSettings)!);
            await File.WriteAllTextAsync(appSettings, "{}", cancellationToken);
        };

        // Act
        await CreateSut().Run();

        // Assert
        Assert.Equal(1, _syncRegistryRuns);
        Assert.Contains("1 files of the project are missing. Run \"Sync Registry With SupportToolsServer...\" now?|True",
            _questions);
        string output = _consoleOutput.ToString();
        Assert.Contains($"Missing file: {appSettings}", output, StringComparison.Ordinal);
        Assert.Contains("Project files: OK", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenStoredFileIsMissingAndSyncRegistryIsDeclined_ReportsTheMissingFile()
    {
        // Arrange
        string seedParameters = Path.Combine(_tempFolder, "Security", "seed.json");
        _project.SeedProjectParametersFilePath = seedParameters;
        _syncRegistryAnswer = false;

        // Act
        await CreateSut().Run();

        // Assert
        Assert.Equal(0, _syncRegistryRuns);
        Assert.Contains($"Project files missing (run \"Sync Registry With SupportToolsServer...\"): {seedParameters}",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Run_WhenFinished_LogsTheResult(bool cloneFails)
    {
        // Arrange
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        if (cloneFails)
        {
            _gitActions.FailingClones.Add(MainGitName);
            _gitActions.FailingClones.Add(SpaGitName);
        }

        // Act
        await CreateSut(logger.Object).Run();

        // Assert
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString() ==
                                                   $"Clone Project To This Computer App finished, success: {!cloneFails}"),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }

    //the logging is skipped when the logger does not log information
    [Fact]
    public async Task Run_WhenInformationIsNotLogged_LogsNothing()
    {
        // Arrange
        var logger = new Mock<ILogger>();

        // Act
        await CreateSut(logger.Object).Run();

        // Assert
        logger.Verify(x => x.IsEnabled(LogLevel.Information), Times.Once);
        logger.VerifyNoOtherCalls();
    }

    //a folder that cannot be created (here: a file is in its way) stops the command before any git work
    [Fact]
    public async Task Run_WhenProjectFolderCannotBeCreated_ReportsItAndDoesNothingElse()
    {
        // Arrange
        string blockingFile = Path.Combine(_tempFolder, "Blocking");
        await File.WriteAllTextAsync(blockingFile, "file");
        string projectFolder = Path.Combine(blockingFile, ProjectName);
        _parameters.Projects[ProjectName] = new ProjectModel
        {
            ProjectFolderName = projectFolder, GitProjectNames = [MainGitName]
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_gitActions.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains($"Folder {projectFolder} cannot be created: ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Clone Project To This Computer App: finished", output, StringComparison.Ordinal);
    }

    private CloneProjectToThisComputerCliMenuCommand CreateSut(ILogger? logger = null)
    {
        return new CloneProjectToThisComputerCliMenuCommand(logger ?? new Mock<ILogger>().Object,
            _parametersManager.Object, ProjectName, _gitActions, InputBool,
            cancellationToken => _syncRegistry(cancellationToken));
    }

    private bool InputBool(string question, bool defaultValue)
    {
        _questions.Add($"{question}|{defaultValue}");
        if (question.StartsWith("Clone also", StringComparison.Ordinal))
        {
            return _scaffoldSeederAnswer;
        }

        return question.StartsWith("Clone ", StringComparison.Ordinal) ? _confirmAnswer : _syncRegistryAnswer;
    }

    private static GitDataModel CreateGit(string gitName, string folderName)
    {
        return new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{gitName}.git",
            GitProjectFolderName = folderName,
            GitIgnorePatternName = "CSharp"
        };
    }

    private static GitProjectDataModel CreateGitProject(string gitName, string projectRelativePath,
        string projectFileName)
    {
        return new GitProjectDataModel
        {
            GitName = gitName, ProjectRelativePath = projectRelativePath, ProjectFileName = projectFileName
        };
    }
}
