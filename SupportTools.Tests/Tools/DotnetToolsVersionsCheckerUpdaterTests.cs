using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Tools;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Tools;

//the dotnet tool commands are replaced by a fake: no network (dotnet tool search) and no global installs.
//Only the public entry points use the real dotnet, and only where no tool is registered
[Collection(ConsoleCaptureCollection.Name)]
public sealed class DotnetToolsVersionsCheckerUpdaterTests : IDisposable
{
    private const string PackageId = "fake.tool";
    private const string ToolKey = "FakeTool";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly FakeDotnetToolsRunner _runner = new();

    public DotnetToolsVersionsCheckerUpdaterTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    private string ConsoleText => _consoleOutput.ToString();

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    //the public entry points use the real dotnet: with no registered tool only the installed tools are listed

    [Fact]
    public void Check_WhenNoToolIsRegistered_ReportsNoChange()
    {
        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object);

        // Assert
        Assert.False(result);
        Assert.Contains("Checking versions for all tools...", ConsoleText, StringComparison.Ordinal);
        Assert.Contains("Checking versions for all tools Finished.", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckOne_WhenToolIsNotRegistered_ReportsItAndFails()
    {
        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.CheckOne(_parametersManager.Object, "Missing");

        // Assert
        Assert.False(result);
        Assert.Contains("Tool with key Missing not found.", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateOne_WhenToolIsNotRegistered_ReportsItAndFails()
    {
        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, "Missing");

        // Assert
        Assert.False(result);
        Assert.Contains("Tool with key Missing not found.", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateAllToolsToLatestVersion_WhenNoToolIsRegistered_ReportsAllUpToDate()
    {
        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager.Object);

        // Assert
        Assert.True(result);
        Assert.Contains("Checking for tools Updates...", ConsoleText, StringComparison.Ordinal);
        Assert.Contains("All tools already are up to date.", ConsoleText, StringComparison.Ordinal);
    }

    //Check

    //Check reported only the change of the last tool, so the change of an earlier one was reported as no change
    [Fact]
    public void Check_WhenOnlyAnEarlierToolChanged_ReportsTheChange()
    {
        // Arrange
        DotnetToolData first = AddTool("First", "first.tool", "0.9.0");
        AddTool("Second", "second.tool", "1.0.0");
        _runner.Install("first.tool", "1.0.0");
        _runner.Install("second.tool", "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("1.0.0", first.InstalledVersion);
        Assert.Contains("Create List of Installed tools...", ConsoleText, StringComparison.Ordinal);
        Assert.Contains("Check versions of tool first.tool...", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_WhenNothingChanged_ReportsNoChange()
    {
        // Arrange
        AddTool("First", "first.tool", "1.0.0");
        AddTool("Second", "second.tool", "1.0.0");
        _runner.Install("first.tool", "1.0.0");
        _runner.Install("second.tool", "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object, _runner);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Check_WhenOnlyTheLatestVersionChanged_ReportsTheChange()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("2.0.0", tool.LatestVersion);
    }

    [Fact]
    public void Check_WhenOnlyTheCommandNameChanged_ReportsTheChange()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        tool.CommandName = "old-command";
        _runner.Install(PackageId, "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal(FakeDotnetToolsRunner.CommandName(PackageId), tool.CommandName);
    }

    //a tool that is not installed has no command name: the stored one stays
    [Fact]
    public void Check_WhenToolIsNotInstalled_KeepsTheStoredCommandName()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        tool.CommandName = "stored-command";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("N/A", tool.InstalledVersion);
        Assert.Equal("stored-command", tool.CommandName);
    }

    [Fact]
    public void Check_WhenToolListFails_PrintsTheErrorAndReportsNoChange()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.ListFailsFromCall = 1;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object, _runner);

        // Assert
        Assert.False(result);
        Assert.Contains(FakeDotnetToolsRunner.FailureDescription, ConsoleText, StringComparison.Ordinal);
        Assert.Contains("Error when Create List Of Dotnet Tools Installed", ConsoleText, StringComparison.Ordinal);
    }

    //one broken record does not stop checking the others; the errors of all records are reported
    [Fact]
    public void Check_WhenOneToolFails_ChecksTheOthersAndReportsTheError()
    {
        // Arrange
        AddTool("Broken", string.Empty, "1.0.0");
        DotnetToolData good = AddTool("Good", PackageId, "0.9.0");
        _runner.Install(PackageId, "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.Check(_parametersManager.Object, _runner);

        // Assert
        Assert.False(result);
        Assert.Equal("1.0.0", good.InstalledVersion);
        Assert.Contains("Package Id Is Empty", ConsoleText, StringComparison.Ordinal);
        Assert.Contains("Error when Check Versions Of package Broken", ConsoleText, StringComparison.Ordinal);
    }

    //CheckOne

    [Fact]
    public void CheckOne_WhenToolIsRegistered_ChecksItsVersions()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.CheckOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("2.0.0", tool.LatestVersion);
        Assert.Contains($"Checking versions for tool {ToolKey}...", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckOne_WhenSearchFails_PrintsTheErrorAndFails()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.SearchFails = true;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.CheckOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.False(result);
        Assert.Contains("Error when detect Available Version Of Tool", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckOne_WhenToolListFails_PrintsTheErrorAndFails()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.ListFailsFromCall = 1;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.CheckOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.False(result);
        Assert.Contains("Error when Create List Of Dotnet Tools Installed", ConsoleText, StringComparison.Ordinal);
    }

    //dotnet tool search answers with a header, a separator line and then "id version authors ..."
    [Theory]
    [InlineData("Package ID      Latest Version", "N/A")]
    [InlineData("Package ID      Latest Version\n--------\nfake.tool", "N/A")]
    [InlineData("Package ID      Latest Version\n--------\nfake.tool      3.0.0", "3.0.0")]
    [InlineData("Package ID      Latest Version\n--------\nfake.tool      3.0.0      Author\n", "3.0.0")]
    public void CheckOne_WhenSearchAnswers_ReadsTheLatestVersionFromTheThirdLine(string searchOutput, string expected)
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        _runner.SearchOutput = searchOutput.Replace("\n", Environment.NewLine, StringComparison.Ordinal);

        // Act
        DotnetToolsVersionsCheckerUpdater.CheckOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.Equal(expected, tool.LatestVersion);
    }

    //dotnet tool list --global answers with two header lines and then "id version command"; other lines are skipped
    [Fact]
    public void CheckOne_WhenToolListHasOtherLines_SkipsThem()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "0.9.0");
        _runner.ExtraListLines.Add("broken line");
        _runner.Install(PackageId, "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.CheckOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("1.0.0", tool.InstalledVersion);
    }

    //UpdateOne

    //a check had already stored the current versions, and UpdateOne used to update only when they changed
    [Fact]
    public void UpdateOne_WhenStoredVersionsAreCurrentButOutdated_UpdatesTheTool()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        tool.LatestVersion = "2.0.0";
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal([$"update {PackageId}"], _runner.Calls);
        Assert.Equal("2.0.0", tool.InstalledVersion);
        Assert.Contains($"updateing {PackageId}...", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateOne_WhenLatestVersionIsInstalled_DoesNotUpdate()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "2.0.0");
        _runner.Install(PackageId, "2.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Empty(_runner.Calls);
    }

    [Fact]
    public void UpdateOne_WhenMaxVersionIsInstalled_DoesNotUpdate()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.5.0");
        tool.MaxVersion = "1.5.0";
        _runner.Install(PackageId, "1.5.0");
        _runner.Latest[PackageId] = "2.0.0";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Empty(_runner.Calls);
    }

    [Fact]
    public void UpdateOne_WhenToolIsNotInstalled_InstallsItsMaxVersion()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "N/A");
        tool.MaxVersion = "1.5.0";
        _runner.Latest[PackageId] = "2.0.0";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal([$"install {PackageId} 1.5.0"], _runner.Calls);
        Assert.Equal("1.5.0", tool.InstalledVersion);
        Assert.Contains($"installing {PackageId}...", ConsoleText, StringComparison.Ordinal);
    }

    //the feed does not know the package: there is nothing to update to
    [Fact]
    public void UpdateOne_WhenLatestVersionIsUnknown_DoesNotUpdate()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest.Remove(PackageId);

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("N/A", tool.LatestVersion);
        Assert.Empty(_runner.Calls);
    }

    //a blank package id passes the check, which wants only a non-empty one, but is never installed
    [Fact]
    public void UpdateOne_WhenPackageIdIsBlank_DoesNotInstall()
    {
        // Arrange
        AddTool(ToolKey, " ", "N/A");
        _runner.Latest[" "] = "2.0.0";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.True(result);
        Assert.Empty(_runner.Calls);
    }

    [Fact]
    public void UpdateOne_WhenCheckFails_PrintsTheErrorAndDoesNotUpdate()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.SearchFails = true;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.False(result);
        Assert.Empty(_runner.Calls);
        Assert.Contains("Error when detect Available Version Of Tool", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateOne_WhenUpdateFails_PrintsTheErrorAndFails()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";
        _runner.UpdateFails = true;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.False(result);
        Assert.Contains(FakeDotnetToolsRunner.FailureDescription, ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateOne_WhenCheckAfterTheUpdateFails_PrintsTheErrorAndFails()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";
        _runner.ListFailsFromCall = 2;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateOne(_parametersManager.Object, ToolKey, _runner);

        // Assert
        Assert.False(result);
        Assert.Equal([$"update {PackageId}"], _runner.Calls);
        Assert.Contains("Error when Create List Of Dotnet Tools Installed", ConsoleText, StringComparison.Ordinal);
    }

    //UpdateAllToolsToLatestVersion

    //only the updated tool changes, so the versions must be checked again after it
    [Fact]
    public void UpdateAllToolsToLatestVersion_WhenOneToolIsOutdated_UpdatesItAndChecksAgain()
    {
        // Arrange
        DotnetToolData outdated = AddTool("Outdated", PackageId, "1.0.0");
        AddTool("Current", "current.tool", "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Install("current.tool", "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager.Object,
            _runner);

        // Assert
        Assert.True(result);
        Assert.Equal([$"update {PackageId}"], _runner.Calls);
        Assert.Equal("2.0.0", outdated.InstalledVersion);
        Assert.Contains("Updating tools List...", ConsoleText, StringComparison.Ordinal);
        Assert.Contains("Updating process Finished.", ConsoleText, StringComparison.Ordinal);
        Assert.DoesNotContain("All tools already are up to date.", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateAllToolsToLatestVersion_WhenNothingIsOutdated_ReportsAllUpToDate()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager.Object,
            _runner);

        // Assert
        Assert.True(result);
        Assert.Empty(_runner.Calls);
        Assert.Contains("All tools already are up to date.", ConsoleText, StringComparison.Ordinal);
        Assert.DoesNotContain("Updating tools List...", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateAllToolsToLatestVersion_WhenCheckFails_PrintsTheErrorAndUpdatesNothing()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.ListFailsFromCall = 1;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager.Object,
            _runner);

        // Assert
        Assert.False(result);
        Assert.Empty(_runner.Calls);
        Assert.Contains("Error when Create List Of Dotnet Tools Installed", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateAllToolsToLatestVersion_WhenAnUpdateFails_PrintsTheErrorAndFails()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";
        _runner.UpdateFails = true;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager.Object,
            _runner);

        // Assert
        Assert.False(result);
        Assert.Contains(FakeDotnetToolsRunner.FailureDescription, ConsoleText, StringComparison.Ordinal);
        Assert.DoesNotContain("Updating tools List...", ConsoleText, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateAllToolsToLatestVersion_WhenCheckAfterTheUpdatesFails_PrintsTheErrorAndFails()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.0.0");
        _runner.Latest[PackageId] = "2.0.0";
        _runner.ListFailsFromCall = 2;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.UpdateAllToolsToLatestVersion(_parametersManager.Object,
            _runner);

        // Assert
        Assert.False(result);
        Assert.Contains("Error when Create List Of Dotnet Tools Installed", ConsoleText, StringComparison.Ordinal);
        Assert.DoesNotContain("Updating process Finished.", ConsoleText, StringComparison.Ordinal);
    }

    //RefreshInstalledVersions (runs whenever the Dotnet Tools list is built: only dotnet tool list, no network)

    [Fact]
    public void RefreshInstalledVersions_WhenInstalledVersionChanged_UpdatesItAndClearsLatestVersion()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "9.0.11");
        tool.LatestVersion = "10.0.0";
        _runner.Install(PackageId, "10.0.12");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("10.0.12", tool.InstalledVersion);
        Assert.Null(tool.LatestVersion);
    }

    [Fact]
    public void RefreshInstalledVersions_WhenNothingChanged_ReportsNoChangeAndKeepsLatestVersion()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        tool.LatestVersion = "2.0.0";
        _runner.Install(PackageId, "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.False(result);
        Assert.Equal("1.0.0", tool.InstalledVersion);
        Assert.Equal("2.0.0", tool.LatestVersion);
    }

    [Fact]
    public void RefreshInstalledVersions_WhenOnlyCommandNameChanged_UpdatesItAndKeepsLatestVersion()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        tool.CommandName = "old-command";
        tool.LatestVersion = "2.0.0";
        _runner.Install(PackageId, "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal(FakeDotnetToolsRunner.CommandName(PackageId), tool.CommandName);
        Assert.Equal("2.0.0", tool.LatestVersion);
    }

    [Fact]
    public void RefreshInstalledVersions_WhenToolIsNotInstalled_MarksItNotAvailable()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Equal("N/A", tool.InstalledVersion);
        Assert.Null(tool.LatestVersion);
    }

    //only the tool that changed loses its latest version
    [Fact]
    public void RefreshInstalledVersions_WhenOneOfTwoToolsChanged_ClearsOnlyItsLatestVersion()
    {
        // Arrange
        DotnetToolData first = AddTool("First", "first.tool", "1.0.0");
        DotnetToolData second = AddTool("Second", "second.tool", "1.0.0");
        _runner.Install("first.tool", "1.1.0");
        _runner.Install("second.tool", "1.0.0");

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.True(result);
        Assert.Null(first.LatestVersion);
        Assert.Equal("1.0.0", second.LatestVersion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RefreshInstalledVersions_WhenPackageIdIsEmpty_LeavesToolUnchanged(string? packageId)
    {
        // Arrange
        var tool = new DotnetToolData { PackageId = packageId, InstalledVersion = "1.0.0", LatestVersion = "2.0.0" };
        _parameters.DotnetTools[ToolKey] = tool;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.False(result);
        Assert.Equal("1.0.0", tool.InstalledVersion);
        Assert.Equal("2.0.0", tool.LatestVersion);
    }

    //the stored data stays as it is; Check Dotnet Tools Versions reports the error
    [Fact]
    public void RefreshInstalledVersions_WhenListFails_ReportsNoChangeAndKeepsStoredData()
    {
        // Arrange
        DotnetToolData tool = AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.1.0");
        _runner.ListFailsFromCall = 1;

        // Act
        bool result = DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.False(result);
        Assert.Equal("1.0.0", tool.InstalledVersion);
        Assert.Equal("1.0.0", tool.LatestVersion);
    }

    //the list is rebuilt after every menu action, so the refresh must stay fast and quiet
    [Fact]
    public void RefreshInstalledVersions_WhenCalled_NeitherSearchesNorPrints()
    {
        // Arrange
        AddTool(ToolKey, PackageId, "1.0.0");
        _runner.Install(PackageId, "1.1.0");

        // Act
        DotnetToolsVersionsCheckerUpdater.RefreshInstalledVersions(_parametersManager.Object, _runner);

        // Assert
        Assert.Equal(0, _runner.SearchCalls);
        Assert.Empty(ConsoleText);
    }

    //the stored record already matches the fake's latest version; the tests install it into the fake themselves
    private DotnetToolData AddTool(string toolKey, string packageId, string installedVersion)
    {
        var tool = new DotnetToolData
        {
            PackageId = packageId,
            InstalledVersion = installedVersion,
            LatestVersion = installedVersion,
            CommandName = FakeDotnetToolsRunner.CommandName(packageId)
        };
        _parameters.DotnetTools[toolKey] = tool;
        _runner.Latest.TryAdd(packageId, installedVersion);
        return tool;
    }

    //works like dotnet tool list --global, dotnet tool search, dotnet tool install and dotnet tool update
    private sealed class FakeDotnetToolsRunner : IDotnetToolsRunner
    {
        public const string FailureDescription = "Fake dotnet failure";

        private static readonly Error Failure = Error.Problem("FakeFailure", FailureDescription);

        private readonly Dictionary<string, string> _installed = [];
        private int _listCalls;

        public List<string> Calls { get; } = [];
        public List<string> ExtraListLines { get; } = [];
        public Dictionary<string, string> Latest { get; } = [];

        //the call of GetToolsRawList (counted from 1) from which on the list fails
        public int ListFailsFromCall { get; set; } = int.MaxValue;
        public int SearchCalls { get; private set; }
        public bool SearchFails { get; set; }
        public string? SearchOutput { get; set; }
        public bool UpdateFails { get; set; }

        public Result<IEnumerable<string>> GetToolsRawList()
        {
            _listCalls++;
            if (_listCalls >= ListFailsFromCall)
            {
                return Failure;
            }

            string[] lines =
            [
                "Package Id      Version      Commands", "-------------------------------------",
                .. _installed.Select(x => $"{x.Key}      {x.Value}      {CommandName(x.Key)}"), .. ExtraListLines
            ];
            return lines;
        }

        public Result<(string, int)> SearchTool(string toolName)
        {
            SearchCalls++;
            if (SearchFails)
            {
                return Failure;
            }

            if (SearchOutput is not null)
            {
                return (SearchOutput, 0);
            }

            List<string> lines = ["Package ID      Latest Version      Authors", "----------------------------"];
            if (Latest.TryGetValue(toolName, out string? latestVersion))
            {
                lines.Add($"{toolName}      {latestVersion}      Author");
            }

            return (string.Join(Environment.NewLine, lines) + Environment.NewLine, 0);
        }

        public Result InstallTool(string packageId, string? version)
        {
            Calls.Add($"install {packageId} {version}".TrimEnd());
            return InstallOrFail(packageId, version);
        }

        public Result UpdateTool(string packageId, string? version)
        {
            Calls.Add($"update {packageId} {version}".TrimEnd());
            return InstallOrFail(packageId, version);
        }

        public static string CommandName(string packageId)
        {
            return $"{packageId}-command";
        }

        public void Install(string packageId, string version)
        {
            _installed[packageId] = version;
        }

        private Result InstallOrFail(string packageId, string? version)
        {
            if (UpdateFails)
            {
                return Failure;
            }

            Install(packageId, version ?? Latest[packageId]);
            return Result.Success();
        }
    }
}
