using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using LibGitData.Models;
using SupportTools.Menu;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu;

//a workspace in a temp folder: {temp}\App is the project folder (P) of project App and {temp}\App\App holds its solution
//App.slnx (S). Next to S are the clones of git Pkg (package project Pkg) and git Front (no project) and AppX, a folder
//that is no git of App; {temp}\Other is outside P. The csproj paths are relative to {S}\App.Api
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectSolutionCheckerTests : IDisposable
{
    private const string PkgReference = @"..\..\Pkg\Pkg.Lib\Pkg.Lib.csproj";
    private const string FarReference = @"..\..\..\Other\Far\Far.csproj";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly string _projectFolder;
    private readonly string _solutionFolder;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ProjectSolutionCheckerTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectFolder = Path.Combine(_tempFolder, "App");
        _solutionFolder = Path.Combine(_projectFolder, "App");
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _projectFolder,
            SolutionFileName = Path.Combine(_solutionFolder, "App.slnx"),
            UseLocalPackages = true,
            GitProjectNames = ["App", "Pkg", "Front", "ReactAppCarcass"]
        };
        _parameters.Projects["Pkg"] = new ProjectModel { ProjectType = EProjectType.IsPackage, UseLocalPackages = true };
        _parameters.Gits["App"] = new GitDataModel { GitProjectFolderName = "App" };
        _parameters.Gits["Pkg"] = new GitDataModel { GitProjectFolderName = "Pkg" };
        _parameters.Gits["Front"] = new GitDataModel { GitProjectFolderName = "Front" };
        //as in the real data, a git inside the clone of the front: it is never a clone of its own next to the solution
        _parameters.Gits["ReactAppCarcass"] = new GitDataModel
        {
            GitProjectFolderName = @"{SpaProjectFolderRelativePath}\src\appcarcass"
        };
    }

    private ProjectModel App => _parameters.Projects["App"];

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void CheckProject_WhenReferencesStayInTheSolutionFolder_ReturnsOk()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", @"..\App.Core\App.Core.csproj");
        WriteProject(@"App.Core\App.Core.csproj");
        WriteSolution("App.Api/App.Api.csproj", "App.Core/App.Core.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.Ok, result.Status);
        Assert.Empty(result.Details);
    }

    //the sibling projects of the solution list are kept there for the switch back from packages to projects; only the
    //references of the solution's own projects count, so even a far project of the list changes nothing
    [Fact]
    public void CheckProject_WhenTheSolutionListsSiblingProjectsThatNothingReferences_ReturnsOk()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj");
        WriteSolution("App.Api/App.Api.csproj", "../Pkg/Pkg.Lib/Pkg.Lib.csproj", "../../Other/Far/Far.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.Ok, result.Status);
    }

    [Fact]
    public void CheckProject_WhenAProjectReferencesAPackageClone_ReturnsNeedsDistributePackagesWithThePackage()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", PkgReference);
        WriteSolution("App.Api/App.Api.csproj", "../Pkg/Pkg.Lib/Pkg.Lib.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsDistributePackages, result.Status);
        Assert.Equal(["Pkg"], result.Details);
    }

    [Fact]
    public void CheckProject_WhenSeveralProjectsReferenceThePackage_NamesItOnce()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", PkgReference);
        WriteProject(@"App.Core\App.Core.csproj", PkgReference, @"..\..\Pkg\Pkg.Other\Pkg.Other.csproj");
        WriteSolution("App.Api/App.Api.csproj", "App.Core/App.Core.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(["Pkg"], result.Details);
    }

    [Fact]
    public void CheckProject_WhenAReferenceLeavesTheParentFolder_ReturnsNeedsAttention()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", FarReference);
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.Equal([$"far reference {Path.Combine(_tempFolder, "Other", "Far", "Far.csproj")}"], result.Details);
    }

    //a problem outweighs a package to distribute, and only the problems are named
    [Fact]
    public void CheckProject_WhenAPackageAndAFarReferenceMeet_ReportsOnlyTheFarOne()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", PkgReference, FarReference);
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.StartsWith("far reference ", Assert.Single(result.Details), StringComparison.Ordinal);
    }

    //Distribute all packages never touches a project with UseLocalPackages off, so its source references are intended
    [Fact]
    public void CheckProject_WhenUseLocalPackagesIsOff_AcceptsReferencesIntoTheParentFolder()
    {
        // Arrange
        App.UseLocalPackages = false;
        WriteProject(@"App.Api\App.Api.csproj", PkgReference, @"..\..\AppX\AppX.Lib\AppX.Lib.csproj");
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.Ok, result.Status);
    }

    [Fact]
    public void CheckProject_WhenUseLocalPackagesIsOff_StillReportsAFarReference()
    {
        // Arrange
        App.UseLocalPackages = false;
        WriteProject(@"App.Api\App.Api.csproj", FarReference);
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
    }

    //a git of the project that is no package (the front, code that only this project uses) is a part of the project
    [Fact]
    public void CheckProject_WhenTheReferencedGitIsNoPackage_ReturnsOk()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", @"..\..\Front\front\front.esproj");
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.Ok, result.Status);
    }

    //AppX starts with the name of the solution folder App, but it is no folder inside it; Distribute all packages takes
    //only the packages of the gits list, so this reference would stay
    [Fact]
    public void CheckProject_WhenTheReferencedFolderIsNoGitOfTheProject_ReturnsNeedsAttention()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", @"..\..\AppX\AppX.Lib\AppX.Lib.csproj");
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.Equal(["AppX is not in the gits list"], result.Details);
    }

    [Fact]
    public void CheckProject_WhenThePackageHasUseLocalPackagesOff_ReturnsNeedsAttention()
    {
        // Arrange
        _parameters.Projects["Pkg"].UseLocalPackages = false;
        WriteProject(@"App.Api\App.Api.csproj", PkgReference);
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.Equal(["package Pkg has UseLocalPackages off"], result.Details);
    }

    [Fact]
    public void CheckProject_WhenTheSolutionFileNameIsEmpty_ReturnsNull()
    {
        // Act
        ProjectSolutionCheckResult? result =
            ProjectSolutionChecker.CheckProject(_parameters, new ProjectModel { ProjectFolderName = _projectFolder });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CheckProject_WhenTheSolutionFileDoesNotExist_ReturnsNeedsAttention()
    {
        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.Equal([$"solution file {App.SolutionFileName} does not exist"], result.Details);
    }

    [Fact]
    public void CheckProject_WhenTheFileIsNoSolution_ReturnsNeedsAttention()
    {
        // Arrange
        string readme = Path.Combine(_tempFolder, "Readme.txt");
        File.WriteAllText(readme, "text");
        var project = new ProjectModel { ProjectFolderName = _projectFolder, SolutionFileName = readme };

        // Act
        ProjectSolutionCheckResult? result = ProjectSolutionChecker.CheckProject(_parameters, project);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.Equal([$"{readme} is not a solution file"], result.Details);
    }

    [Fact]
    public void CheckProject_WhenTheSolutionIsNotXml_ReturnsNeedsAttention()
    {
        // Arrange
        Directory.CreateDirectory(_solutionFolder);
        File.WriteAllText(Path.Combine(_solutionFolder, "App.slnx"), "not xml");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.StartsWith($"cannot read {App.SolutionFileName}: ", Assert.Single(result.Details),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CheckProject_WhenAnOwnProjectOfTheSolutionIsMissing_ReturnsNeedsAttention()
    {
        // Arrange
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.Equal([$"project {Path.Combine(_solutionFolder, "App.Api", "App.Api.csproj")} does not exist"],
            result.Details);
    }

    [Fact]
    public void CheckProject_WhenAnOwnProjectIsNotXml_ReturnsNeedsAttention()
    {
        // Arrange
        string projectFile = WriteProject(@"App.Api\App.Api.csproj");
        File.WriteAllText(projectFile, "not xml");
        WriteSolution("App.Api/App.Api.csproj");

        // Act
        ProjectSolutionCheckResult result = CheckSolution();

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention, result.Status);
        Assert.StartsWith($"cannot read {projectFile}: ", Assert.Single(result.Details), StringComparison.Ordinal);
    }

    [Fact]
    public void CheckProjects_WhenCalled_StoresTheResultsAndWritesALinePerProject()
    {
        // Arrange
        WriteProject(@"App.Api\App.Api.csproj", PkgReference);
        WriteSolution("App.Api/App.Api.csproj");
        _parameters.Projects["NoSolution"] = new ProjectModel { ProjectFolderName = _projectFolder };

        // Act
        ProjectSolutionChecker.CheckProjects(_parameters, _parameters.Projects.Where(x => x.Key != "Pkg"),
            _menuParameters, CancellationToken.None);

        // Assert
        Assert.Equal(EProjectSolutionCheckStatus.NeedsDistributePackages,
            _menuParameters.ProjectSolutionCheckResults["App"].Status);
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("NoSolution"));
        string output = _consoleOutput.ToString();
        Assert.Contains("App: Needs Distribute Packages (Pkg)", output, StringComparison.Ordinal);
        Assert.Contains("NoSolution: solution file name is not specified, skipped", output, StringComparison.Ordinal);
    }

    //a project that has no solution file any more keeps no old result
    [Fact]
    public void CheckProjects_WhenAProjectHasNoSolutionAnyMore_RemovesItsOldResult()
    {
        // Arrange
        _menuParameters.ProjectSolutionCheckResults["NoSolution"] =
            new ProjectSolutionCheckResult(EProjectSolutionCheckStatus.Ok, []);
        _parameters.Projects["NoSolution"] = new ProjectModel { ProjectFolderName = _projectFolder };

        // Act
        ProjectSolutionChecker.CheckProjects(_parameters, _parameters.Projects.Where(x => x.Key == "NoSolution"),
            _menuParameters, CancellationToken.None);

        // Assert
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("NoSolution"));
    }

    private ProjectSolutionCheckResult CheckSolution()
    {
        ProjectSolutionCheckResult? result = ProjectSolutionChecker.CheckProject(_parameters, App);
        Assert.NotNull(result);
        return result;
    }

    //writes a project file at a path relative to S with the given ProjectReference paths and returns its full path
    private string WriteProject(string pathFromSolutionFolder, params string[] references)
    {
        string projectFile = Path.GetFullPath(Path.Combine(_solutionFolder, pathFromSolutionFolder));
        Directory.CreateDirectory(Path.GetDirectoryName(projectFile)!);
        string items = string.Concat(references.Select(reference => $"""<ProjectReference Include="{reference}" />"""));
        File.WriteAllText(projectFile, $"""<Project Sdk="Microsoft.NET.Sdk"><ItemGroup>{items}</ItemGroup></Project>""");
        return projectFile;
    }

    private void WriteSolution(params string[] projectPaths)
    {
        Directory.CreateDirectory(_solutionFolder);
        string items = string.Concat(projectPaths.Select(projectPath => $"""<Project Path="{projectPath}" />"""));
        File.WriteAllText(Path.Combine(_solutionFolder, "App.slnx"), $"<Solution>{items}</Solution>");
    }
}
