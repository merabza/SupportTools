using System.Collections.Generic;
using System.Linq;
using LibGitData.Models;
using LibSupportToolsServerWork.GitProjects;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.GitProjects;

//The server returns every project of every repository, ordered by git, path and file (SupportToolsServer's B9); the
//mapper gives what the local Update Git Projects would
public sealed class ServerGitProjectsMapperTests
{
    private static readonly PathMapper WindowsPathMapper = new([], '\\');

    private static StsGitProjectDataModel ServerProject(string gitName, string projectRelativePath,
        string projectFileName, params string[] dependsOnProjectNames)
    {
        return new StsGitProjectDataModel
        {
            GitName = gitName,
            ProjectRelativePath = projectRelativePath,
            ProjectFileName = projectFileName,
            DependsOnProjectNames = [.. dependsOnProjectNames]
        };
    }

    private static string Describe(GitProjectDataModel gitProject)
    {
        return
            $"{gitProject.GitName}|{gitProject.ProjectRelativePath}|{gitProject.ProjectFileName}|{string.Join(",", gitProject.DependsOnProjectNames)}";
    }

    //The key is the file name without extension, as in the client's GitProjectsUpdater.RegisterProject
    [Fact]
    public void ToLocal_WhenServerHasProjects_KeysThemByTheFileNameWithoutExtension()
    {
        // Arrange
        List<StsGitProjectDataModel> serverProjects =
        [
            ServerProject("AppGrammarGe", @"AppGrammarGe\AppGrammarGe", "AppGrammarGe.csproj", "LibA", "LibB"),
            ServerProject("AppGrammarGeFront", @"AppGrammarGeFront\appgrammargefrontend", "appgrammargefrontend.esproj")
        ];

        // Act
        ServerGitProjects result =
            ServerGitProjectsMapper.ToLocal(serverProjects, ["AppGrammarGe", "AppGrammarGeFront"], WindowsPathMapper);

        // Assert
        Assert.Equal(["AppGrammarGe", "appgrammargefrontend"], result.GitProjects.Keys.Order());
        Assert.Equal(@"AppGrammarGe|AppGrammarGe\AppGrammarGe|AppGrammarGe.csproj|LibA,LibB",
            Describe(result.GitProjects["AppGrammarGe"]));
        Assert.Equal(@"AppGrammarGeFront|AppGrammarGeFront\appgrammargefrontend|appgrammargefrontend.esproj|",
            Describe(result.GitProjects["appgrammargefrontend"]));
        Assert.Equal(".esproj", result.GitProjects["appgrammargefrontend"].ProjectExtension);
        Assert.Empty(result.Duplicates);
        Assert.Empty(result.UnknownGitNames);
    }

    //Like the local scan, the gits are processed in name order and the project of the later git keeps the name; the
    //duplicate is reported. The real case: SystemTools.DependencyInjection of CrawlerConsole and of SystemTools
    [Fact]
    public void ToLocal_WhenTwoGitsHaveAProjectOfTheSameName_KeepsTheOneOfTheLaterGit()
    {
        // Arrange
        List<StsGitProjectDataModel> serverProjects =
        [
            ServerProject("SystemTools", @"SystemTools\SystemTools.DependencyInjection",
                "SystemTools.DependencyInjection.csproj", "SystemTools.SystemToolsShared"),
            ServerProject("CrawlerConsole", @"CrawlerConsole\SystemTools.DependencyInjection",
                "SystemTools.DependencyInjection.csproj")
        ];

        // Act
        ServerGitProjects result = ServerGitProjectsMapper.ToLocal(serverProjects,
            ["SystemTools", "CrawlerConsole"], WindowsPathMapper);

        // Assert
        GitProjectDataModel kept = Assert.Single(result.GitProjects.Values);
        Assert.Equal(
            @"SystemTools|SystemTools\SystemTools.DependencyInjection|SystemTools.DependencyInjection.csproj|SystemTools.SystemToolsShared",
            Describe(kept));
        Assert.Equal(
            new GitProjectDuplicate("SystemTools.DependencyInjection", "CrawlerConsole", "SystemTools"),
            Assert.Single(result.Duplicates));
    }

    //Within one git the later project in the order of the server keeps the name
    [Fact]
    public void ToLocal_WhenOneGitHasAProjectNameTwice_KeepsTheLaterOne()
    {
        // Arrange
        List<StsGitProjectDataModel> serverProjects =
        [
            ServerProject("RepoA", @"RepoA\Old\FakeHost", "FakeHost.csproj"),
            ServerProject("RepoA", @"RepoA\New\FakeHost", "FakeHost.csproj")
        ];

        // Act
        ServerGitProjects result = ServerGitProjectsMapper.ToLocal(serverProjects, ["RepoA"], WindowsPathMapper);

        // Assert
        Assert.Equal(@"RepoA\New\FakeHost", Assert.Single(result.GitProjects.Values).ProjectRelativePath);
        Assert.Equal(new GitProjectDuplicate("FakeHost", "RepoA", "RepoA"), Assert.Single(result.Duplicates));
    }

    //The local scan covers the local gits only: the projects of another git are left out and the git is named once.
    //A server name matches the local key without case, and the local spelling is kept
    [Fact]
    public void ToLocal_WhenAGitIsNotLocal_SkipsItsProjectsAndKeepsTheLocalSpellingOfTheOthers()
    {
        // Arrange
        List<StsGitProjectDataModel> serverProjects =
        [
            ServerProject("OnlyOnServer", @"OnlyOnServer\AppX", "AppX.csproj"),
            ServerProject("OnlyOnServer", @"OnlyOnServer\AppY", "AppY.csproj"),
            ServerProject("REPOA", @"RepoA\AppA", "AppA.csproj")
        ];

        // Act
        ServerGitProjects result = ServerGitProjectsMapper.ToLocal(serverProjects, ["RepoA"], WindowsPathMapper);

        // Assert
        Assert.Equal("RepoA", Assert.Single(result.GitProjects.Values).GitName);
        Assert.Equal(["OnlyOnServer"], result.UnknownGitNames);
    }

    //The relative path is canonical (\); on Linux only the separators change, like GitProjectFolderName
    [Fact]
    public void ToLocal_WhenComputerIsLinux_StoresLinuxSeparators()
    {
        // Arrange
        List<StsGitProjectDataModel> serverProjects =
            [ServerProject("RepoA", @"RepoA\Libs\LibA", "LibA.csproj")];

        // Act
        ServerGitProjects result =
            ServerGitProjectsMapper.ToLocal(serverProjects, ["RepoA"], new PathMapper([], '/'));

        // Assert
        Assert.Equal("RepoA/Libs/LibA", result.GitProjects["LibA"].ProjectRelativePath);
    }

    //The local model gets its own list of dependencies
    [Fact]
    public void ToLocal_CopiesTheDependencies()
    {
        // Arrange
        StsGitProjectDataModel serverProject = ServerProject("RepoA", @"RepoA\AppA", "AppA.csproj", "LibA");

        // Act
        ServerGitProjects result = ServerGitProjectsMapper.ToLocal([serverProject], ["RepoA"], WindowsPathMapper);
        serverProject.DependsOnProjectNames.Add("LibB");

        // Assert
        Assert.Equal(["LibA"], result.GitProjects["AppA"].DependsOnProjectNames);
    }

    [Fact]
    public void ToLocal_WhenServerHasNoProjects_ReturnsNone()
    {
        // Act
        ServerGitProjects result = ServerGitProjectsMapper.ToLocal([], ["RepoA"], WindowsPathMapper);

        // Assert
        Assert.Empty(result.GitProjects);
        Assert.Empty(result.Duplicates);
        Assert.Empty(result.UnknownGitNames);
    }

    //The order of the dependencies is no change: locally it depends on the earlier scans, on the server it is the order
    //of the names
    [Fact]
    public void CountChanges_CountsTheAddedChangedAndRemovedProjects()
    {
        // Arrange
        var oldGitProjects = new Dictionary<string, GitProjectDataModel>
        {
            ["Kept"] = LocalProject("RepoA", @"RepoA\Kept", "Kept.csproj", "LibB", "LibA"),
            ["OtherDependencies"] = LocalProject("RepoA", @"RepoA\Other", "OtherDependencies.csproj", "LibA"),
            ["Moved"] = LocalProject("RepoA", @"RepoA\Moved", "Moved.csproj"),
            ["OtherGit"] = LocalProject("RepoA", @"RepoA\OtherGit", "OtherGit.csproj"),
            ["Renamed"] = LocalProject("RepoA", @"RepoA\Renamed", "Renamed.csproj"),
            ["Removed"] = LocalProject("RepoA", @"RepoA\Removed", "Removed.csproj")
        };
        var newGitProjects = new Dictionary<string, GitProjectDataModel>
        {
            ["Kept"] = LocalProject("RepoA", @"RepoA\Kept", "Kept.csproj", "LibA", "LibB"),
            ["OtherDependencies"] = LocalProject("RepoA", @"RepoA\Other", "OtherDependencies.csproj", "LibC"),
            ["Moved"] = LocalProject("RepoA", @"RepoA\Libs\Moved", "Moved.csproj"),
            ["OtherGit"] = LocalProject("RepoB", @"RepoA\OtherGit", "OtherGit.csproj"),
            ["Renamed"] = LocalProject("RepoA", @"RepoA\Renamed", "Renamed.esproj"),
            ["Added"] = LocalProject("RepoA", @"RepoA\Added", "Added.csproj")
        };

        // Act
        (int added, int changed, int removed) = ServerGitProjectsMapper.CountChanges(oldGitProjects, newGitProjects);

        // Assert
        Assert.Equal((1, 4, 1), (added, changed, removed));
    }

    private static GitProjectDataModel LocalProject(string gitName, string projectRelativePath, string projectFileName,
        params string[] dependsOnProjectNames)
    {
        return new GitProjectDataModel
        {
            GitName = gitName,
            ProjectRelativePath = projectRelativePath,
            ProjectFileName = projectFileName,
            DependsOnProjectNames = [.. dependsOnProjectNames]
        };
    }
}
