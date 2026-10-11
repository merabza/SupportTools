using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.DistributeAllPackages;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.DistributeAllPackages;

//no git, dotnet or package server runs: the package projects have no gits in Gits (their sync does nothing) and no
//solution file, so the first processed package stops the run at the build check
[Collection(ConsoleCaptureCollection.Name)]
public sealed class DistributeAllPackagesCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public DistributeAllPackagesCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
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
        var sut = new DistributeAllPackagesCliMenuCommand(new Mock<ILogger>().Object, "SupportTools",
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, _menuParameters);

        // Assert
        Assert.Equal("Distribute all packages", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task GetActionDescription_WhenCalled_DescribesTheWholeProcess()
    {
        // Act
        string? result = await CliMenuTestAccess.InvokeGetActionDescription(CreateSut());

        // Assert
        Assert.Equal(
            "This process will sync gits, check build, check solution, build and distribute packages for all package type projects, ordered by package dependencies",
            result);
    }

    [Fact]
    public async Task RunBody_WhenThereAreNoPackageProjects_WarnsAndSucceeds()
    {
        // Arrange
        AddProject("App", EProjectType.Standard, true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
        Assert.Contains("No package type projects found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenOnlyRemotePackagesExistAndAreNotIncluded_WarnsAndSucceeds()
    {
        // Arrange
        AddProject("RemotePackage", EProjectType.IsPackage, false);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([("Include 1 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        Assert.Contains("No package type projects found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRemotePackagesAreIncluded_ProcessesThemToo()
    {
        // Arrange
        AddProject("RemotePackage", EProjectType.IsPackage, false);
        _answer = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("Package projects processing order: RemotePackage", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //a package depends on the packages whose repositories are in its gits list; the ready ones go in name order
    [Fact]
    public async Task RunBody_WhenPackagesDependOnEachOther_OrdersThemByTheirDependencies()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Alpha", "Core");
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Beta", EProjectType.IsPackage, true, "Beta", "Tool");
        AddProject("Tool", EProjectType.Standard, true);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("Package projects processing order: Beta, Core, Alpha" + Environment.NewLine,
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a repeated git counts once, so the package does not wait for a second, never coming, dependency
    [Fact]
    public async Task RunBody_WhenADependencyIsListedTwice_CountsItOnce()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Core", "Core");
        AddProject("Core", EProjectType.IsPackage, true);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("Package projects processing order: Core, Alpha" + Environment.NewLine,
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenPackagesDependOnEachOtherInACycle_ReportsTheCycleAndFails()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Beta");
        AddProject("Beta", EProjectType.IsPackage, true, "Alpha");
        AddProject("Gamma", EProjectType.IsPackage, true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        Assert.Contains(
            "Cannot order package projects by dependencies, cycle detected. Projects in cycle: Alpha, Beta" +
            Environment.NewLine, output, StringComparison.Ordinal);
        Assert.DoesNotContain("Package projects processing order", output, StringComparison.Ordinal);
    }

    //the first step that fails stops the whole run
    [Fact]
    public async Task RunBody_WhenBuildCheckFails_StopsOnTheFirstPackage()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        AddProject("Beta", EProjectType.IsPackage, true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(EProjectBuildCheckStatus.SolutionFileNameIsEmpty,
            _menuParameters.ProjectBuildCheckResults["Alpha"].Status);
        Assert.False(_menuParameters.ProjectBuildCheckResults.ContainsKey("Beta"));
        string output = _consoleOutput.ToString();
        Assert.Contains("Processing package project Alpha (1/2)", output, StringComparison.Ordinal);
        Assert.Contains("Sync Multiple Projects Gits V2 Started", output, StringComparison.Ordinal);
        Assert.Contains("Build check failed for project Alpha with status SolutionFileNameIsEmpty", output,
            StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages stopped on project Alpha. Successfully processed 0 of 2 package projects", output,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Processing package project Beta", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenCancelled_StopsBeforeTheFirstPackage()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CliMenuTestAccess.InvokeRunBody(CreateSut(), cancellationTokenSource.Token));

        // Assert
        Assert.DoesNotContain("Processing package project Alpha", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private DistributeAllPackagesCliMenuCommand CreateSut()
    {
        return new DistributeAllPackagesCliMenuCommand(new Mock<ILogger>().Object, "SupportTools",
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, _menuParameters,
            (question, defaultValue) =>
            {
                _questions.Add((question, defaultValue));
                return _answer;
            });
    }

    private void AddProject(string projectName, EProjectType projectType, bool isLocal, params string[] gitNames)
    {
        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectType = projectType,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing"),
            GitProjectNames = [.. gitNames]
        };
    }
}
