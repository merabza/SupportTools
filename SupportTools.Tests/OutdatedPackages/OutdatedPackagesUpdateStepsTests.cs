using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.OutdatedPackages;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.OutdatedPackages;

//the real V3 sync over a throwaway repository (a local bare remote, so no network is needed) and without
//SupportToolsServer (no ApiClient name); no dotnet outdated runs: the updated gits are not C# gits or not in Gits
[Collection(ConsoleCaptureCollection.Name)]
public sealed class OutdatedPackagesUpdateStepsTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _projectFolder;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public OutdatedPackagesUpdateStepsTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectFolder = Path.Combine(_tempFolder, "PR");
        Directory.CreateDirectory(_projectFolder);
        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        _parameters.Projects["PR"] = new ProjectModel { ProjectFolderName = _projectFolder, GitProjectNames = ["GitR"] };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public async Task SyncGits_WhenAGitHasLocalChanges_CommitsAndPushesThemWithTheGivenMessage()
    {
        // Arrange
        string remote = CreateRemoteAndClone();
        await File.WriteAllTextAsync(Path.Combine(_projectFolder, "GitR", "New.txt"), "new");

        // Act
        bool result = await CreateSut().SyncGits("PR", false, "Update message Prepare", CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal("Update message Prepare", GitTestRepositories.RunGit("-C", remote, "log", "-1", "--format=%s"));
        Assert.Contains("Sync Multiple Projects Gits V3 Started", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //V3 refuses a folder whose origin is not the address of its git
    [Fact]
    public async Task SyncGits_WhenAFolderFails_ReturnsFalse()
    {
        // Arrange
        CreateRemoteAndClone();
        _parameters.Gits["GitR"].GitProjectAddress = Path.Combine(_tempFolder, "Remotes", "Other.git");

        // Act
        bool result = await CreateSut().SyncGits(null, false, "Update message Check", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    //only a C# git runs restore and dotnet outdated
    [Fact]
    public void UpdatePackages_WhenTheGitIsNotACSharpGit_ReturnsSuccess()
    {
        // Arrange
        AddGit("GitR", "git@github.com:x/GitR.git");

        // Act
        IReadOnlyList<EPackageUpdateResult> results =
            CreateSut().UpdatePackages([new OutdatedPackagesUpdateStep("PR", "GitR")], CancellationToken.None);

        // Assert
        Assert.Equal([EPackageUpdateResult.Success], results);
    }

    //reported with the failures of dotnet outdated; the other gits of the level are still updated
    [Fact]
    public void UpdatePackages_WhenAGitIsNotInGits_ReturnsOutdatedFailedForIt()
    {
        // Arrange
        AddGit("GitR", "git@github.com:x/GitR.git");
        _parameters.Projects["PR"].GitProjectNames.Add("GitMissing");

        // Act
        IReadOnlyList<EPackageUpdateResult> results = CreateSut().UpdatePackages(
            [new OutdatedPackagesUpdateStep("PR", "GitMissing"), new OutdatedPackagesUpdateStep("PR", "GitR")],
            CancellationToken.None);

        // Assert
        Assert.Equal([EPackageUpdateResult.OutdatedFailed, EPackageUpdateResult.Success], results);
        Assert.Contains("GitSyncParameters is not created", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private OutdatedPackagesUpdateSteps CreateSut()
    {
        return new OutdatedPackagesUpdateSteps(new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object, null);
    }

    private string CreateRemoteAndClone()
    {
        string remote = GitTestRepositories.CreateRemote(_tempFolder, "GitR",
            new Dictionary<string, string> { [".gitignore"] = "bin/", ["Readme.txt"] = "GitR" });
        AddGit("GitR", remote);
        string clone = Path.Combine(_projectFolder, "GitR");
        GitTestRepositories.RunGit("clone", remote, clone);
        GitTestRepositories.ConfigureRepository(clone);
        return remote;
    }

    private void AddGit(string gitName, string address)
    {
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = address, GitProjectFolderName = gitName, GitIgnorePatternName = "Node"
        };
    }
}
