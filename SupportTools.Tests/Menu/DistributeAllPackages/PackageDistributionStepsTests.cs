using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.DistributeAllPackages;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.DistributeAllPackages;

//the real steps on cases that never reach git, dotnet or a package server: project App has no gits and no solution
//file, and the package steps stop at once for a project that is not registered
[Collection(ConsoleCaptureCollection.Name)]
public sealed class PackageDistributionStepsTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly PackageDistributionSteps _sut;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public PackageDistributionStepsTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectType = EProjectType.IsPackage, ProjectFolderName = _tempFolder
        };
        var parametersManager = new Mock<IParametersManager>();
        parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _sut = new PackageDistributionSteps(new Mock<ILogger>().Object, "SupportTools",
            new Mock<IHttpClientFactory>().Object, parametersManager.Object, _menuParameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    //the V3 sync of the project, with the console
    [Fact]
    public async Task SyncProjectGits_WhenProjectHasNoGits_RunsTheSyncOnTheConsoleAndSucceeds()
    {
        // Act
        bool result = await _sut.SyncProjectGits("App", "Message", CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Contains("Sync Multiple Projects Gits V3 Started", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a project that is not registered has nothing to compare: its packages are built, and the build steps report it
    [Fact]
    public async Task GetUnchangedUploadedPackageVersion_WhenProjectIsNotRegistered_ReturnsNullWithoutAWord()
    {
        // Act
        string? result = await _sut.GetUnchangedUploadedPackageVersion("Missing", CancellationToken.None);

        // Assert
        Assert.Null(result);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    //the result is the one the build check writes into the menu parameters
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CheckBuild_WhenProjectHasNoSolution_ReturnsAndKeepsTheCheckResult(bool noIncremental)
    {
        // Act
        ProjectBuildCheckResult? result =
            _sut.CheckBuild("App", _parameters.Projects["App"], noIncremental, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EProjectBuildCheckStatus.SolutionFileNameIsEmpty, result.Status);
        Assert.Same(result, _menuParameters.ProjectBuildCheckResults["App"]);
    }

    [Fact]
    public void CheckPackageSolution_WhenProjectIsNotRegistered_Fails()
    {
        // Act
        bool result = _sut.CheckPackageSolution("Missing");

        // Assert
        Assert.False(result);
        Assert.Contains("Project Missing does not found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAndUploadPackage_WhenProjectIsNotRegistered_Fails()
    {
        // Act
        bool result = _sut.BuildAndUploadPackage("Missing");

        // Assert
        Assert.False(result);
        Assert.Contains("Project Missing does not found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DistributePackagesToProject_WhenAPackageProjectIsNotRegistered_Fails()
    {
        // Act
        bool result = await _sut.DistributePackagesToProject("App", ["Missing"], CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Contains("Project Missing does not found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }
}
