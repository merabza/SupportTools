using System.Collections.Generic;
using System.Linq;
using LibGitData.Models;
using LibGitWork.OutdatedPackages;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.OutdatedPackages;

//a git that is not in Gits counts as a C# git, so most tests give only the gits of other types
public sealed class OutdatedPackagesUpdateOrderTests
{
    private readonly Dictionary<string, GitDataModel> _gits = [];
    private readonly Dictionary<string, ProjectModel> _projects = [];

    [Fact]
    public void Create_WhenThereIsNoProject_ReturnsNoStep()
    {
        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Empty(steps);
    }

    [Fact]
    public void Create_WhenAProjectUsesTheGitOfAnother_UpdatesTheUsedGitFirst()
    {
        // Arrange
        AddProject("App", "App", "Lib");
        AddProject("Lib", "Lib");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["Lib/Lib", "App/App"], steps);
    }

    //B gets down to one git in the first round, but it waits for the second one, so C goes before it
    [Fact]
    public void Create_WhenProjectsHaveOneGitLeft_UpdatesThemRoundByRoundInNameOrder()
    {
        // Arrange
        AddProject("C", "GitC");
        AddProject("B", "GitA", "GitB");
        AddProject("A", "GitA");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["A/GitA", "C/GitC", "B/GitB"], steps);
    }

    [Fact]
    public void Create_WhenTwoProjectsHaveTheSameSingleGit_UpdatesItOnceInTheFirstProject()
    {
        // Arrange
        AddProject("P2", "GitA");
        AddProject("P1", "GitA");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["P1/GitA"], steps);
    }

    //before the fix App never got down to one git because of its front git, and was never updated
    [Fact]
    public void Create_WhenAProjectHasGitsOfAnotherType_DoesNotWaitForThem()
    {
        // Arrange
        AddGit("App", "CSharp");
        AddGit("AppFront", "React");
        AddProject("App", "App", "AppFront");
        AddProject("Front", "AppFront");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["App/App"], steps);
    }

    //their update fails, so the failure is reported instead of the git being left out silently
    [Fact]
    public void Create_WhenAGitIsNotInGitsOrHasNoType_KeepsItInTheOrder()
    {
        // Arrange
        AddGit("NoType", null);
        AddProject("P1", "Missing");
        AddProject("P2", "NoType");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["P1/Missing", "P2/NoType"], steps);
    }

    //the real case: SupportToolsServerShared has no project of its own, and both projects wait for it
    [Fact]
    public void Create_WhenNoProjectHasOneGitLeft_UpdatesTheMostUsedGitFirstInItsFirstProject()
    {
        // Arrange
        AddProject("SupportToolsServer", "SupportToolsServer", "SupportToolsServerShared");
        AddProject("SupportTools", "SupportTools", "SupportToolsServerShared");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(
        [
            "SupportTools/SupportToolsServerShared", "SupportTools/SupportTools",
            "SupportToolsServer/SupportToolsServer"
        ], steps);
    }

    //the real case: AppMimosiGeShared has no project of its own; by name alone AppMimosiGe would go first
    [Fact]
    public void Create_WhenTheLeftGitsAreEquallyUsed_UpdatesTheGitThatIsNotAProjectNameFirst()
    {
        // Arrange
        AddProject("AppMimosiGe", "AppMimosiGe", "AppMimosiGeShared");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["AppMimosiGe/AppMimosiGeShared", "AppMimosiGe/AppMimosiGe"], steps);
    }

    [Fact]
    public void Create_WhenTheLeftGitsAreEquallyUsedAndNoneIsAProjectName_UpdatesThemInNameOrder()
    {
        // Arrange
        AddProject("X", "GitB", "GitA");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["X/GitA", "X/GitB"], steps);
    }

    [Fact]
    public void Create_WhenGitNamesDifferOnlyInCase_TreatsThemAsOneGit()
    {
        // Arrange
        AddProject("P1", "GitA");
        AddProject("P2", "gita", "GitB");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["P1/GitA", "P2/GitB"], steps);
    }

    //the git is looked up in the list of its project with its case, so the step keeps the spelling of that project
    [Fact]
    public void Create_WhenTheMostUsedGitIsSpelledDifferently_UsesTheSpellingOfTheProjectThatUpdatesIt()
    {
        // Arrange
        AddProject("S2", "S2", "Shared");
        AddProject("S1", "S1", "shared");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["S1/shared", "S1/S1", "S2/S2"], steps);
    }

    [Fact]
    public void Create_WhenAProjectListsAGitTwice_UpdatesItOnce()
    {
        // Arrange
        AddProject("P", "GitA", "GitA");

        // Act
        List<string> steps = CreateSteps();

        // Assert
        Assert.Equal(["P/GitA"], steps);
    }

    //a round is a level: its gits do not wait for each other, so they are updated in parallel
    [Fact]
    public void Create_WhenProjectsHaveOneGitLeft_PutsEveryRoundIntoALevel()
    {
        // Arrange
        AddProject("C", "GitC");
        AddProject("B", "GitA", "GitB");
        AddProject("A", "GitA");

        // Act
        List<string> levels = CreateLevels();

        // Assert
        Assert.Equal(["A/GitA, C/GitC", "B/GitB"], levels);
    }

    //the most used git goes alone, then the projects that waited for it are one level
    [Fact]
    public void Create_WhenNoProjectHasOneGitLeft_PutsTheMostUsedGitIntoALevelOfItsOwn()
    {
        // Arrange
        AddProject("SupportToolsServer", "SupportToolsServer", "SupportToolsServerShared");
        AddProject("SupportTools", "SupportTools", "SupportToolsServerShared");

        // Act
        List<string> levels = CreateLevels();

        // Assert
        Assert.Equal(
        [
            "SupportTools/SupportToolsServerShared",
            "SupportTools/SupportTools, SupportToolsServer/SupportToolsServer"
        ], levels);
    }

    private List<string> CreateSteps()
    {
        return
        [
            .. OutdatedPackagesUpdateOrder.Create(_projects, _gits).SelectMany(x => x)
                .Select(x => $"{x.ProjectName}/{x.GitName}")
        ];
    }

    private List<string> CreateLevels()
    {
        return
        [
            .. OutdatedPackagesUpdateOrder.Create(_projects, _gits)
                .Select(x => string.Join(", ", x.Select(y => $"{y.ProjectName}/{y.GitName}")))
        ];
    }

    private void AddGit(string gitName, string? gitIgnorePatternName)
    {
        _gits[gitName] = new GitDataModel
        {
            GitProjectFolderName = gitName, GitIgnorePatternName = gitIgnorePatternName
        };
    }

    private void AddProject(string projectName, params string[] gitNames)
    {
        _projects[projectName] = new ProjectModel { GitProjectNames = [.. gitNames] };
    }
}
