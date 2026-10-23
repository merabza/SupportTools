using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using LibGitWork.SyncV3;
using LibGitWork.ToolActions;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.Tests.SyncV3;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//runs the real V3 sync over throwaway repositories (local bare remotes, so no network is needed) and a simulated
//SupportToolsServer. G1: Local1 (folder exists, git RepoA) and Remote1 (folder missing, git RepoB); G2: Local2 (folder
//exists, git RepoC)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncMultipleProjectsGitsToolActionV3Tests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string HeadsRequest = "GET /api/v1/git/gitremoteheads";
    private readonly Queue<bool> _boolAnswers = new();
    private readonly List<(string Question, bool DefaultValue)> _boolQuestions = [];

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly string _projectsFolder;
    private readonly Dictionary<string, string> _remotes = [];
    private readonly RoutingHttpMessageHandler _server = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private readonly Queue<string> _textAnswers = new();
    private readonly List<(string Question, string? DefaultValue)> _textQuestions = [];

    public SyncMultipleProjectsGitsToolActionV3Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectsFolder = Path.Combine(_tempFolder, "Projects");
        //without WorkFolder the work folder clones are left out; the tests of the work folder clones set it
        foreach (string gitName in (string[])["RepoA", "RepoB", "RepoC"])
        {
            AddGit(gitName);
        }

        AddProject("Local1", "G1", true, "RepoA");
        AddProject("Remote1", "G1", false, "RepoB");
        AddProject("Local2", "G2", true, "RepoC");
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _server.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public async Task Run_WhenThereIsNoServer_ClonesTheGitsOfTheLocalProjects()
    {
        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(Path.Combine(GitFolder("Local1", "RepoA"), ".git")));
        Assert.True(Directory.Exists(Path.Combine(GitFolder("Local2", "RepoC"), ".git")));
        Assert.False(Directory.Exists(ProjectFolder("Remote1")));
        string output = _consoleOutput.ToString();
        Assert.Contains("Sync Multiple Projects Gits V3 Started", output, StringComparison.Ordinal);
        Assert.Contains("SupportToolsServerWebApiClientName is not specified: every git is checked with fetch", output,
            StringComparison.Ordinal);
        Assert.Contains("Checked 2 folders: 0 with changes, 2 to clone, 0 failed", output, StringComparison.Ordinal);
        Assert.Contains($"RepoA  {GitFolder("Local1", "RepoA")}: cloned", output, StringComparison.Ordinal);
        Assert.Contains(
            $"{Environment.NewLine}Synced 2 gits, 2 folders: 0 without a request to the git server, 0 fetched, 2 cloned, 0 committed, 0 pulled, 0 pushed, 0 failed",
            output, StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    //SupportToolsServer still knows the commit of the folder, although the remote moved on: the folder is not fetched.
    //The sync of one project asks only for its gits
    [Fact]
    public async Task Run_WhenTheServerKnowsTheHeadOfTheFolder_LeavesItWithoutFetching()
    {
        // Arrange
        UseServer();
        string folder = CloneInto("Local1", "RepoA");
        string knownCommit = Head(folder);
        PushNewCommit("RepoA");
        RespondHeads(("RepoA", "main", knownCommit));

        // Act
        bool result = await Create(null, "Local1", true).Run();

        // Assert
        Assert.True(result);
        Assert.Equal(knownCommit, Head(folder));
        Assert.Equal([(HeadsRequest, "?names=RepoA")], _server.Requests.Select(x => (x.Request, x.Query)));
        string output = _consoleOutput.ToString();
        Assert.Contains("Remote heads from SupportToolsServer: 1 of 1 gits", output, StringComparison.Ordinal);
        Assert.Contains("1 folders: 1 without a request to the git server, 0 fetched", output,
            StringComparison.Ordinal);
        //the server answered during the local check, and it has the head of every git
        Assert.DoesNotContain("Waiting for the remote heads", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Checked with fetch", output, StringComparison.Ordinal);
        //nothing happened to the folder, so it gets no result line
        Assert.DoesNotContain($"RepoA  {folder}: {Environment.NewLine}", output, StringComparison.Ordinal);
    }

    //every git without a head is named
    [Fact]
    public async Task Run_WhenTheServerHasNoHeadOfTwoGits_NamesBoth()
    {
        // Arrange
        UseServer();
        RespondHeads();

        // Act
        await Create("G1", null, true).Run();

        // Assert
        Assert.Contains("Checked with fetch: RepoA, RepoB", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenTheFolderIsBehindTheHeadOfTheServer_FetchesAndPulls()
    {
        // Arrange
        UseServer();
        string folder = CloneInto("Local1", "RepoA");
        string newCommit = PushNewCommit("RepoA");
        RespondHeads(("RepoA", "main", newCommit));

        // Act
        await Create(null, "Local1", true).Run();

        // Assert
        Assert.Equal(newCommit, Head(folder));
        Assert.Contains($"RepoA  {folder}: pulled", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //without the heads of the server every git is fetched, so the new commit comes all the same
    [Fact]
    public async Task Run_WhenTheServerDoesNotAnswer_FetchesEveryGit()
    {
        // Arrange
        UseServer();
        _server.Respond(HeadsRequest, HttpStatusCode.InternalServerError,
            """{"title":"Db","status":500,"detail":"Database failure"}""");
        string folder = CloneInto("Local1", "RepoA");
        string newCommit = PushNewCommit("RepoA");

        // Act
        bool result = await Create(null, "Local1", true).Run();

        // Assert
        Assert.True(result);
        Assert.Equal(newCommit, Head(folder));
        string output = _consoleOutput.ToString();
        Assert.Contains("SupportToolsServer did not return the remote heads (Database failure)", output,
            StringComparison.Ordinal);
        Assert.Contains("1 folders: 0 without a request to the git server, 1 fetched", output,
            StringComparison.Ordinal);
    }

    //a git that the server has no complete head of is fetched, and its name is shown
    [Theory]
    [InlineData(null, null)]
    [InlineData("main", null)]
    [InlineData(null, "1dfa466db703de783bc9929e9bd765f2c870a1f2")]
    public async Task Run_WhenTheServerHasNoHeadOfAGit_NamesItAndFetchesIt(string? branchName, string? commitId)
    {
        // Arrange
        UseServer();
        string folder = CloneInto("Local1", "RepoA");
        RespondHeads(("RepoA", branchName, commitId));

        // Act
        await Create(null, "Local1", true).Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Remote heads from SupportToolsServer: 0 of 1 gits", output, StringComparison.Ordinal);
        Assert.Contains("Checked with fetch: RepoA", output, StringComparison.Ordinal);
        Assert.True(Directory.Exists(folder));
    }

    //the sync of every project asks for the heads of every git without names; a group names its gits
    [Fact]
    public async Task Run_WhenAllProjectsOrAGroupAreSynced_AsksForEveryGitOrForTheGitsOfTheGroup()
    {
        // Arrange
        UseServer();
        RespondHeads();

        // Act
        await Create(null, null, false).Run();
        await Create("G1", null, true).Run();

        // Assert
        Assert.Equal([(HeadsRequest, ""), (HeadsRequest, "?names=RepoA&names=RepoB")],
            _server.Requests.Select(x => (x.Request, x.Query)));
        Assert.True(Directory.Exists(GitFolder("Remote1", "RepoB")));
    }

    //the given message is used without asking; SupportToolsServer learns about the push
    [Fact]
    public async Task Run_WhenAFolderHasChanges_CommitsWithTheGivenMessagePushesAndNotifiesTheServer()
    {
        // Arrange
        UseServer();
        RespondHeads();
        string folder = CloneInto("Local2", "RepoC");
        await File.WriteAllTextAsync(Path.Combine(folder, "New.txt"), "new");

        // Act
        bool result = await Create(null, "Local2", true, "Sync message").Run();

        // Assert
        Assert.True(result);
        Assert.Equal("Sync message", GitTestRepositories.RunGit("-C", _remotes["RepoC"], "log", "-1", "--format=%s"));
        Assert.Contains("POST /api/v1/git/gitpushed/RepoC", _server.Requests.Select(x => x.Request));
        Assert.Empty(_textQuestions);
        string output = _consoleOutput.ToString();
        Assert.Contains($"RepoC  {folder}: has changes", output, StringComparison.Ordinal);
        Assert.Contains($"RepoC  {folder}: committed, pushed", output, StringComparison.Ordinal);
    }

    //a branch made here and never pushed is published under its name; the line names it, the summary counts a push
    [Fact]
    public async Task Run_WhenAFolderIsOnABranchWithoutUpstream_PublishesTheBranch()
    {
        // Arrange
        string folder = CloneInto("Local2", "RepoC");
        GitTestRepositories.RunGit("-C", folder, "checkout", "-q", "-b", "feature");
        await File.WriteAllTextAsync(Path.Combine(folder, "New.txt"), "new");

        // Act
        bool result = await Create(null, "Local2", true, "Sync message").Run();

        // Assert
        Assert.True(result);
        Assert.Equal(Head(folder), GitTestRepositories.RunGit("-C", _remotes["RepoC"], "rev-parse", "feature"));
        string output = _consoleOutput.ToString();
        Assert.Contains($"RepoC  {folder}: committed, published branch feature", output, StringComparison.Ordinal);
        Assert.Contains("1 committed, 0 pulled, 1 pushed, 0 failed", output, StringComparison.Ordinal);
    }

    //the message is asked once, before the parallel sync, and kept for the next folders on request
    [Fact]
    public async Task Run_WhenTheMessageIsKept_AsksOnceForEveryChangedFolder()
    {
        // Arrange
        string folderA = CloneInto("Local1", "RepoA");
        string folderC = CloneInto("Local2", "RepoC");
        await File.WriteAllTextAsync(Path.Combine(folderA, "A.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(folderC, "C.txt"), "c");
        _textAnswers.Enqueue("Typed message");
        _boolAnswers.Enqueue(true);

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        (string question, string? defaultValue) = Assert.Single(_textQuestions);
        Assert.Equal("Message", question);
        //the first default is the time stamp yyyyMMddHHmm
        Assert.Matches("^[0-9]{12}$", defaultValue);
        Assert.Contains($"Commit message for RepoA  {folderA}", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal([("Use this message for next commits?", true)], _boolQuestions);
        Assert.Equal("Typed message", GitTestRepositories.RunGit("-C", _remotes["RepoA"], "log", "-1", "--format=%s"));
        Assert.Equal("Typed message", GitTestRepositories.RunGit("-C", _remotes["RepoC"], "log", "-1", "--format=%s"));
    }

    //a new message for every folder; the last folder is not asked whether to keep its message
    [Fact]
    public async Task Run_WhenTheMessageIsNotKept_AsksForEveryChangedFolder()
    {
        // Arrange
        string folderA = CloneInto("Local1", "RepoA");
        string folderC = CloneInto("Local2", "RepoC");
        await File.WriteAllTextAsync(Path.Combine(folderA, "A.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(folderC, "C.txt"), "c");
        _textAnswers.Enqueue("Message A");
        _textAnswers.Enqueue("Message C");
        _boolAnswers.Enqueue(false);

        // Act
        await Create(null, null, false).Run();

        // Assert
        Assert.Equal(2, _textQuestions.Count);
        Assert.Equal("Message A", _textQuestions[1].DefaultValue);
        Assert.Single(_boolQuestions);
        Assert.Equal("Message A", GitTestRepositories.RunGit("-C", _remotes["RepoA"], "log", "-1", "--format=%s"));
        Assert.Equal("Message C", GitTestRepositories.RunGit("-C", _remotes["RepoC"], "log", "-1", "--format=%s"));
    }

    [Fact]
    public async Task Run_WhenAFolderIsClonedFromAnotherAddress_ReportsItAndFails()
    {
        // Arrange
        string folder = GitFolder("Local1", "RepoA");
        GitTestRepositories.RunGit("clone", _remotes["RepoC"], folder);

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        //the error of the local check is written once, not again with the results of the sync
        Assert.Single(output.Split($"RepoA  {folder}: is cloned from {_remotes["RepoC"]}, not from {_remotes["RepoA"]}")
            .Skip(1));
        Assert.Contains(
            "[warning] Synced 2 gits, 2 folders: 0 without a request to the git server, 0 fetched, 1 cloned, 0 committed, 0 pulled, 0 pushed, 1 failed",
            output, StringComparison.Ordinal);
        //the summary is written once, as a warning
        Assert.Single(output.Split("Synced 2 gits").Skip(1));
    }

    //a folder that committed but could not push gets only its error line: the commit is no result to show
    [Fact]
    public async Task Run_WhenAFolderCommitsButCannotPush_ShowsOnlyTheError()
    {
        // Arrange
        string folder = CloneInto("Local2", "RepoC");
        await File.WriteAllTextAsync(Path.Combine(folder, "New.txt"), "new");
        await File.WriteAllTextAsync(Path.Combine(_remotes["RepoC"], "hooks", "pre-receive"), "#!/bin/sh\nexit 1\n");

        // Act
        bool result = await Create(null, "Local2", true, "Sync message").Run();

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        Assert.Contains($"RepoC  {folder}: git push failed", output, StringComparison.Ordinal);
        Assert.DoesNotContain($"RepoC  {folder}: committed", output, StringComparison.Ordinal);
        Assert.Contains("1 committed, 0 pulled, 0 pushed, 1 failed", output, StringComparison.Ordinal);
    }

    //the errors of the gits list (here a git without an address) are shown on the console
    [Fact]
    public async Task Run_WhenAGitHasNoAddress_ReportsItOnTheConsole()
    {
        // Arrange
        _parameters.Gits["NoAddress"] = new GitDataModel
        {
            GitProjectFolderName = "NoAddress", GitIgnorePatternName = "CSharp"
        };

        // Act
        await Create(null, "Local1", false).Run();

        // Assert
        Assert.Contains("GitProjectAddress is empty for Git Repo with key NoAddress)", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //the changed gits start first, then the others by name, without case; the folders of a git stay together
    [Fact]
    public void GroupByGit_WhenCalled_PutsTheChangedGitsFirstAndTheOthersInNameOrder()
    {
        // Arrange
        List<GitFolderInspection> inspections =
        [
            Inspection("RepoC", "C", false), Inspection("RepoB", "B1", false), Inspection("repoa", "A", false),
            Inspection("repob", "B2", true)
        ];

        // Act
        List<IGrouping<string, GitFolderInspection>>
            gits = SyncMultipleProjectsGitsToolActionV3.GroupByGit(inspections);

        // Assert
        Assert.Equal(["RepoB", "repoa", "RepoC"], gits.Select(x => x.Key));
        Assert.Equal(["B1", "B2"], gits[0].Select(x => x.Folder.ProjectName));
    }

    [Fact]
    public async Task Run_WhenThereIsNoFolderToSync_SaysSoAndSucceeds()
    {
        // Act
        bool result = await Create(null, "Missing", true).Run();

        // Assert
        Assert.True(result);
        Assert.Contains("No git folders to sync", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //two projects in the same folder: the folder is checked and synced once
    [Fact]
    public async Task Run_WhenTwoProjectsShareAFolder_SyncsItOnce()
    {
        // Arrange
        _parameters.Projects["Local3"] = new ProjectModel
        {
            ProjectGroupName = "G2", ProjectFolderName = ProjectFolder("Local2"), GitProjectNames = ["RepoC"]
        };

        // Act
        await Create("G2", null, false).Run();

        // Assert
        Assert.Contains("Checked 1 folders", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a git that is not in Gits gives no folder; the other gits of the project are synced
    [Fact]
    public async Task Run_WhenAProjectGitIsNotInGits_SkipsIt()
    {
        // Arrange
        _parameters.Projects["Local1"].GitProjectNames.Add("MissingGit");

        // Act
        bool result = await Create(null, "Local1", false).Run();

        // Assert
        Assert.True(result);
        Assert.Contains("Synced 1 gits, 1 folders", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the ApiClient that SupportToolsServerWebApiClientName names does not exist: a warning, then git itself
    [Fact]
    public async Task Run_WhenTheApiClientIsMissing_WarnsAndFetches()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = "MissingClient";
        CloneInto("Local1", "RepoA");

        // Act
        bool result = await Create(null, "Local1", true).Run();

        // Assert
        Assert.True(result);
        Assert.Contains("ApiClient with name MissingClient does not exists: every git is checked with fetch",
            _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    //without an IHttpClientFactory SupportToolsServer is not used and nothing is said about it
    [Fact]
    public async Task Run_WhenThereIsNoHttpClientFactory_DoesNotAskTheServer()
    {
        // Arrange
        UseServer();
        CloneInto("Local1", "RepoA");
        var syncParameters = SyncMultipleProjectsGitsParametersV2.Create(_parameters, null, "Local1", true);
        var sut = new SyncMultipleProjectsGitsToolActionV3(null, null, _parameters, syncParameters, null,
            (_, _) => "never", (_, _) => false, null);

        // Act
        bool result = await sut.Run();

        // Assert
        Assert.True(result);
        Assert.Empty(_server.Requests);
        Assert.DoesNotContain("Remote heads from SupportToolsServer", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //SupportToolsServer answers after the local check: the console says what the sync waits for
    [Fact]
    public async Task Run_WhenTheServerAnswersLate_SaysThatItWaits()
    {
        // Arrange
        UseServer();
        string folder = CloneInto("Local1", "RepoA");
        RespondHeads(("RepoA", "main", Head(folder)));
        _server.Delay = TimeSpan.FromSeconds(1.5);

        // Act
        await Create(null, "Local1", true).Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Waiting for the remote heads from SupportToolsServer...", output, StringComparison.Ordinal);
        Assert.Contains("Remote heads from SupportToolsServer: 1 of 1 gits", output, StringComparison.Ordinal);
    }

    //the push itself succeeded, so a refused notification is reported, but the sync does not fail
    [Fact]
    public async Task Run_WhenTheServerRefusesThePushNotification_ReportsIt()
    {
        // Arrange
        UseServer();
        RespondHeads();
        _server.Respond("POST /api/v1/git/gitpushed/RepoC", HttpStatusCode.NotFound,
            """{"title":"GitWithKeyNotFound","status":404,"detail":"Git With Key RepoC Not Found"}""");
        string folder = CloneInto("Local2", "RepoC");
        await File.WriteAllTextAsync(Path.Combine(folder, "New.txt"), "new");
        var logger = new Mock<ILogger>();

        // Act
        bool result = await Create(null, "Local2", true, "Sync message", logger.Object).Run();

        // Assert
        Assert.True(result);
        const string message =
            "RepoC: SupportToolsServer did not take the push notification: Git With Key RepoC Not Found";
        Assert.Contains(message, _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifyLog(logger, LogLevel.Error, message, Times.Once());
    }

    //an error of the sync itself (here a clone from a missing remote) is logged as an error
    [Fact]
    public async Task Run_WhenTheSyncOfAFolderFails_LogsTheErrorAsAnError()
    {
        // Arrange
        _parameters.Gits["RepoB"].GitProjectAddress = Path.Combine(_tempFolder, "Remotes", "Missing.git");
        var logger = new Mock<ILogger>();

        // Act
        await Create(null, "Remote1", true, null, logger.Object).Run();

        // Assert
        VerifyLogStartsWith(logger, LogLevel.Error, $"RepoB  {GitFolder("Remote1", "RepoB")}: git clone -- ",
            Times.Once());
    }

    //the board shows every running job, its steps and the counters, and writes the results above it; errors in red
    [Fact]
    public async Task Run_WhenThereIsAConsole_ShowsTheJobsOnTheBoard()
    {
        // Arrange
        UseServer();
        RespondHeads();
        string folderC = CloneInto("Local2", "RepoC");
        await File.WriteAllTextAsync(Path.Combine(folderC, "New.txt"), "new");
        string folderA = GitFolder("Local1", "RepoA");
        GitTestRepositories.RunGit("clone", _remotes["RepoC"], folderA);
        var console = new FakeProgressConsole(300, 60);

        // Act
        await Create(null, null, false, "Sync message", null, console).Run();

        // Assert
        List<string> texts = [.. console.Writes.Select(x => x.Text.TrimEnd())];
        Assert.Contains("RepoC  start", texts);
        Assert.Contains("Checking 2 folders: 0 done", texts);
        Assert.Contains("Checking 2 folders: 2 done", texts);
        Assert.Contains($"RepoC  check  {folderC}", texts);
        Assert.Contains("Syncing 2 gits: 0 done", texts);
        Assert.Contains("Syncing 2 gits: 2 done", texts);
        Assert.Contains($"RepoC  push  {folderC}", texts);
        Assert.Contains("RepoC  notify SupportToolsServer", texts);
        Assert.Contains(($"RepoC  {folderC}: has changes", (ConsoleColor?)null), console.Writes);
        Assert.Contains(($"RepoC  {folderC}: committed, pushed", (ConsoleColor?)null), console.Writes);
        Assert.Contains(
            ($"RepoA  {folderA}: is cloned from {_remotes["RepoC"]}, not from {_remotes["RepoA"]}",
                (ConsoleColor?)ConsoleColor.Red), console.Writes);
    }

    //with LogGitWork the results go to the log as well: errors as errors, the rest only when information is logged
    [Fact]
    public async Task Run_WhenALoggerIsGiven_LogsTheResults()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        string folderC = CloneInto("Local2", "RepoC");
        await File.WriteAllTextAsync(Path.Combine(folderC, "New.txt"), "new");
        string folderA = GitFolder("Local1", "RepoA");
        GitTestRepositories.RunGit("clone", _remotes["RepoC"], folderA);

        // Act
        await Create(null, null, false, "Sync message", logger.Object).Run();

        // Assert
        VerifyLog(logger, LogLevel.Information, $"RepoC  {folderC}: has changes", Times.Once());
        VerifyLog(logger, LogLevel.Information, $"RepoC  {folderC}: committed, pushed", Times.Once());
        VerifyLog(logger, LogLevel.Information, "Checked 2 folders: 1 with changes, 0 to clone, 1 failed",
            Times.Once());
        VerifyLog(logger, LogLevel.Error,
            $"RepoA  {folderA}: is cloned from {_remotes["RepoC"]}, not from {_remotes["RepoA"]}", Times.Once());
    }

    [Fact]
    public async Task Run_WhenTheLoggerDoesNotLogInformation_LogsOnlyTheErrors()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        string folderA = GitFolder("Local1", "RepoA");
        GitTestRepositories.RunGit("clone", _remotes["RepoC"], folderA);

        // Act
        await Create(null, "Local1", false, null, logger.Object).Run();

        // Assert
        VerifyLog(logger, LogLevel.Information, "Checked 1 folders: 0 with changes, 0 to clone, 1 failed",
            Times.Never());
        VerifyLog(logger, LogLevel.Error,
            $"RepoA  {folderA}: is cloned from {_remotes["RepoC"]}, not from {_remotes["RepoA"]}", Times.Once());
    }

    //Create gives the logger to the action only when the git work is logged
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WhenGitWorkIsLoggedOrNot_GivesTheLoggerToTheActionOnlyWhenLogged(bool logGitWork)
    {
        // Arrange
        _parameters.LogGitWork = logGitWork;
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var parametersManager = new Mock<IParametersManager>();
        parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);

        // Act
        await SyncMultipleProjectsGitsToolActionV3.Create(logger.Object, _httpClientFactory.Object,
            parametersManager.Object, null, "Missing", true).Run();

        // Assert
        VerifyLog(logger, LogLevel.Information, "No git folders to sync", logGitWork ? Times.Once() : Times.Never());
    }

    //the commit message of Create comes from the console: in the test host Console.ReadKey fails after the prompt,
    //and the action reports the failure
    [Fact]
    public async Task Create_WhenNoMessageIsGiven_AsksForItOnTheConsole()
    {
        // Arrange
        string folder = CloneInto("Local1", "RepoA");
        await File.WriteAllTextAsync(Path.Combine(folder, "A.txt"), "a");
        var parametersManager = new Mock<IParametersManager>();
        parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);

        // Act
        bool result = await SyncMultipleProjectsGitsToolActionV3.Create(null, _httpClientFactory.Object,
            parametersManager.Object, null, "Local1", true).Run();

        // Assert
        Assert.False(result);
        Assert.Contains("Message", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenThereIsNoWorkFolder_WarnsThatTheWorkFolderClonesAreNotSynced()
    {
        // Act
        await Create(null, "Local1", false).Run();

        // Assert
        Assert.Contains("WorkFolder is not specified: the work folder clones are not synced", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //V2 updates the work folder clone of every git it syncs, so V3 does it too: a missing one is cloned
    [Fact]
    public async Task Run_WhenTheWorkFolderCloneIsMissing_ClonesIt()
    {
        // Arrange
        UseWorkFolder();
        CloneInto("Local1", "RepoA");

        // Act
        bool result = await Create(null, "Local1", false).Run();

        // Assert
        Assert.True(result);
        Assert.Equal(Head(_remotes["RepoA"]), Head(WorkFolderClone("RepoA")));
        string output = _consoleOutput.ToString();
        Assert.Contains($"RepoA  {WorkFolderClone("RepoA")}: cloned", output, StringComparison.Ordinal);
        Assert.Contains("Synced 1 gits, 2 folders", output, StringComparison.Ordinal);
    }

    //the work folder clone is the last folder of its git: it takes the push of the project folder, so V2 has nothing
    //to pull afterwards
    [Fact]
    public async Task Run_WhenAFolderPushes_TheWorkFolderCloneTakesThePush()
    {
        // Arrange
        UseWorkFolder();
        UseServer();
        string folder = CloneInto("Local2", "RepoC");
        string workClone = CloneWorkFolderClone("RepoC");
        RespondHeads(("RepoC", "main", Head(folder)));
        await File.WriteAllTextAsync(Path.Combine(folder, "New.txt"), "new");

        // Act
        bool result = await Create(null, "Local2", true, "Sync message").Run();

        // Assert
        Assert.True(result);
        Assert.Equal(Head(folder), Head(workClone));
        Assert.Equal(Head(_remotes["RepoC"]), Head(workClone));
        Assert.Contains($"RepoC  {workClone}: pulled", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a work folder clone on the head of the server makes no request
    [Fact]
    public async Task Run_WhenTheWorkFolderCloneIsOnTheHeadOfTheServer_LeavesItWithoutFetching()
    {
        // Arrange
        UseWorkFolder();
        UseServer();
        CloneInto("Local1", "RepoA");
        CloneWorkFolderClone("RepoA");
        RespondHeads(("RepoA", "main", Head(_remotes["RepoA"])));

        // Act
        await Create(null, "Local1", false).Run();

        // Assert
        Assert.Contains("2 folders: 2 without a request to the git server", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //nothing is committed in the work folder clone: its changes are an error, the project folder is synced
    [Fact]
    public async Task Run_WhenTheWorkFolderCloneHasChanges_ReportsItWithoutCommitting()
    {
        // Arrange
        UseWorkFolder();
        CloneInto("Local1", "RepoA");
        string workClone = CloneWorkFolderClone("RepoA");
        await File.WriteAllTextAsync(Path.Combine(workClone, "New.txt"), "new");
        string remoteHead = Head(_remotes["RepoA"]);

        // Act
        bool result = await Create(null, "Local1", false).Run();

        // Assert
        Assert.False(result);
        Assert.Equal(remoteHead, Head(_remotes["RepoA"]));
        Assert.Empty(_textQuestions);
        Assert.Contains(
            $"RepoA  {workClone}: is the work folder clone of SupportTools and has changes: restore it (V2 offers to) and sync again",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private void UseWorkFolder()
    {
        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
    }

    private string WorkFolderClone(string gitName)
    {
        return Path.Combine(_tempFolder, "Work", "Gits", gitName);
    }

    private string CloneWorkFolderClone(string gitName)
    {
        string folder = WorkFolderClone(gitName);
        GitTestRepositories.RunGit("clone", _remotes[gitName], folder);
        GitTestRepositories.ConfigureRepository(folder);
        return folder;
    }

    private static GitFolderInspection Inspection(string gitName, string projectName, bool hasChanges)
    {
        return new GitFolderInspection(new GitSyncFolder(gitName, "address", projectName, $@"D:\{projectName}"),
            EGitFolderInspectionKind.Ready, new GitFolderStatus("abc", "main", false, hasChanges), null);
    }

    //a Moq verify of an extension method: the arguments only describe the expected call, nothing is logged
    private static void VerifyLogStartsWith(Mock<ILogger> logger, LogLevel logLevel, string textStart, Times times)
    {
#pragma warning disable CA1873
        logger.Verify(
            x => x.Log(logLevel, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString()!.StartsWith(textStart, StringComparison.Ordinal)),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
#pragma warning restore CA1873
    }

    //a Moq verify of an extension method: the arguments only describe the expected call, nothing is logged
    private static void VerifyLog(Mock<ILogger> logger, LogLevel logLevel, string text, Times times)
    {
#pragma warning disable CA1873
        logger.Verify(
            x => x.Log(logLevel, It.IsAny<EventId>(), It.Is<It.IsAnyType>((value, _) => value.ToString() == text),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
#pragma warning restore CA1873
    }

    private SyncMultipleProjectsGitsToolActionV3 Create(string? projectGroupName, string? projectName,
        bool includeRemoteProjects, string? useMessage = null, ILogger? logger = null,
        IProgressConsole? progressConsole = null)
    {
        var syncParameters = SyncMultipleProjectsGitsParametersV2.Create(_parameters, projectGroupName, projectName,
            includeRemoteProjects);
        return new SyncMultipleProjectsGitsToolActionV3(logger, _httpClientFactory.Object, _parameters, syncParameters,
            useMessage, (question, defaultValue) =>
            {
                _textQuestions.Add((question, defaultValue));
                return _textAnswers.Dequeue();
            }, (question, defaultValue) =>
            {
                _boolQuestions.Add((question, defaultValue));
                return _boolAnswers.Dequeue();
            }, progressConsole);
    }

    private void UseServer()
    {
        _parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/api/v1" };
    }

    private void RespondHeads(params (string GitName, string? BranchName, string? CommitId)[] heads)
    {
        _server.Respond(HeadsRequest, HttpStatusCode.OK,
            JsonSerializer.Serialize(heads.Select(x => new StsGitRemoteHeadDataModel
            {
                GitName = x.GitName,
                BranchName = x.BranchName,
                CommitId = x.CommitId,
                ErrorDescription = x.CommitId is null ? "Remote head cannot be read" : null
            }).ToList()));
    }

    private void AddGit(string gitName)
    {
        //the sync reads the cached files that .gitignore excludes, so every repository has one
        string remote = GitTestRepositories.CreateRemote(_tempFolder, gitName,
            new Dictionary<string, string> { ["Readme.txt"] = gitName, [".gitignore"] = "bin/" });
        _remotes[gitName] = remote;
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = remote, GitProjectFolderName = gitName, GitIgnorePatternName = "CSharp"
        };
    }

    private void AddProject(string projectName, string groupName, bool isLocal, string gitName)
    {
        string projectFolder = ProjectFolder(projectName);
        if (isLocal)
        {
            Directory.CreateDirectory(projectFolder);
        }

        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName, ProjectFolderName = projectFolder, GitProjectNames = [gitName]
        };
    }

    private string CloneInto(string projectName, string gitName)
    {
        string folder = GitFolder(projectName, gitName);
        GitTestRepositories.RunGit("clone", _remotes[gitName], folder);
        GitTestRepositories.ConfigureRepository(folder);
        return folder;
    }

    //another computer pushes a commit to the remote
    private string PushNewCommit(string gitName)
    {
        string other = Path.Combine(_tempFolder, "Other", gitName);
        GitTestRepositories.RunGit("clone", _remotes[gitName], other);
        GitTestRepositories.ConfigureRepository(other);
        File.WriteAllText(Path.Combine(other, "Other.txt"), "other");
        GitTestRepositories.RunGit("-C", other, "add", "--all");
        GitTestRepositories.RunGit("-C", other, "commit", "-m", "Other computer");
        GitTestRepositories.RunGit("-C", other, "push", "origin", "HEAD:main");
        return Head(other);
    }

    private static string Head(string folder)
    {
        return GitTestRepositories.RunGit("-C", folder, "rev-parse", "HEAD");
    }

    private string ProjectFolder(string projectName)
    {
        return Path.Combine(_projectsFolder, projectName);
    }

    private string GitFolder(string projectName, string gitName)
    {
        return Path.Combine(ProjectFolder(projectName), gitName);
    }

    //remembers every request ("METHOD path" and query) and answers by "METHOD path" (200 without a body by default)
    private sealed class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _responses = [];

        public List<(string Request, string Query)> Requests { get; } = [];

        //how long every answer waits
        public TimeSpan Delay { get; set; }

        public void Respond(string request, HttpStatusCode statusCode, string body)
        {
            _responses[request] = (statusCode, body);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            lock (Requests)
            {
                Requests.Add((key, request.RequestUri.Query));
            }

            await Task.Delay(Delay, cancellationToken);
            if (!_responses.TryGetValue(key, out (HttpStatusCode StatusCode, string Body) response))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
            }

            return new HttpResponseMessage(response.StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(response.Body, Encoding.UTF8,
                    response.StatusCode == HttpStatusCode.OK ? "application/json" : "application/problem+json")
            };
        }
    }
}
