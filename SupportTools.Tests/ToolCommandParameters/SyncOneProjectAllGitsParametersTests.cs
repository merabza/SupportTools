using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGitData;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.ToolCommandParameters;
using SupportTools.Tests.GitProjects;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolCommandParameters;

//project Front (folder D:\Projects\Front): gits FrontBack and FrontSpa ({SpaProjectFolderRelativePath}\ClientApp,
//SpaProjectName FrontApp); ScaffoldSeeder git FrontSeeder
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncOneProjectAllGitsParametersTests : IDisposable
{
    private const string ProjectName = "Front";
    private const string ProjectFolder = @"D:\Projects\Front";

    private readonly Dictionary<EGitCollect, Dictionary<string, List<string>>> _changedGitProjects = new()
    {
        [EGitCollect.Collect] = []
    };

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new() { GitExecutablePath = @"C:\Tools\git.exe" };

    public SyncOneProjectAllGitsParametersTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Projects[ProjectName] = new ProjectModel
        {
            ProjectFolderName = ProjectFolder,
            SpaProjectName = "FrontApp",
            ScaffoldSeederProjectName = ProjectName,
            GitProjectNames = ["FrontBack", "FrontSpa"],
            ScaffoldSeederGitProjectNames = ["FrontSeeder"]
        };
        _parameters.ScaffoldSeedersWorkFolder = @"D:\Seeders";
        _parameters.Gits["FrontBack"] = CreateGit("FrontBack", "FrontBack");
        _parameters.Gits["FrontSpa"] =
            CreateGit("FrontSpa", $@"{GitDataModel.SpaProjectFolderRelativePathName}\ClientApp");
        _parameters.Gits["FrontSeeder"] = CreateGit("FrontSeeder", "FrontSeeder");
        _parameters.Gits["Other"] = CreateGit("Other", "Other");
        _parameters.GitProjects["FrontApp"] = new GitProjectDataModel
        {
            GitName = "FrontBack", ProjectRelativePath = @"FrontBack\FrontApp", ProjectFileName = "FrontApp.esproj"
        };
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Create_WhenMainGits_ReturnsTheProjectGitsAndTheGivenValues()
    {
        // Act
        SyncOneProjectAllGitsParameters? result = Create(EGitCol.Main);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProjectName, result.ProjectName);
        Assert.Equal(ProjectFolder, result.GitsFolder);
        Assert.Equal(["FrontBack", Path.Combine(@"FrontBack\FrontApp", "ClientApp")],
            result.GitData.Select(x => x.GitProjectFolderName));
        Assert.Same(_changedGitProjects, result.ChangedGitProjects);
        Assert.True(result.IsFirstSync);
        Assert.False(result.UseProjectUpdater);
        Assert.Equal(@"C:\Tools\git.exe", result.GitExecutablePath);
        Assert.True(result.CheckBeforeSave());
        Assert.Empty(_consoleOutput.ToString());
    }

    [Fact]
    public void Create_WhenScaffoldSeederGits_ReturnsTheSeederGitsAndFolder()
    {
        // Act
        SyncOneProjectAllGitsParameters? result = Create(EGitCol.ScaffoldSeed);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.Combine(@"D:\Seeders", ProjectName, "FrontScaffoldSeeder"), result.GitsFolder);
        Assert.Equal(["FrontSeeder"], result.GitData.Select(x => x.GitProjectFolderName));
    }

    [Fact]
    public void Create_WhenProjectDoesNotExist_ReportsItAndReturnsNull()
    {
        // Act
        var result = SyncOneProjectAllGitsParameters.Create(null, _parameters,
            "Missing", EGitCol.Main, null, false, true);

        // Assert
        Assert.Null(result);
        AssertErrorLine("project is not found");
    }

    [Fact]
    public void Create_WhenGitsFolderIsUnknown_ReportsItAndReturnsNull()
    {
        // Arrange
        _parameters.ScaffoldSeedersWorkFolder = null;

        // Act
        SyncOneProjectAllGitsParameters? result = Create(EGitCol.ScaffoldSeed);

        // Assert
        Assert.Null(result);
        AssertErrorLine("Gits folder is not found");
    }

    [Fact]
    public void Create_WhenSomeGitsAreAbsent_ListsThemAndReturnsTheOthers()
    {
        // Arrange
        _parameters.Gits.Remove("FrontBack");

        // Act
        SyncOneProjectAllGitsParameters? result = Create(EGitCol.Main);

        // Assert
        Assert.NotNull(result);
        Assert.Equal([Path.Combine(@"FrontBack\FrontApp", "ClientApp")],
            result.GitData.Select(x => x.GitProjectFolderName));
        AssertErrorLine("FrontBack");
        AssertErrorLine("Gits with this names are absent");
    }

    [Fact]
    public void Create_WhenSpaRecordIsMissing_ReturnsNullWithClearError()
    {
        // Arrange
        _parameters.GitProjects.Clear();

        // Act
        SyncOneProjectAllGitsParameters? result = Create(EGitCol.Main);

        // Assert
        Assert.Null(result);
        AssertErrorLine(SpaProjectFolderHelperTests.MissingMessage);
    }

    private SyncOneProjectAllGitsParameters? Create(EGitCol gitCol)
    {
        return SyncOneProjectAllGitsParameters.Create(null, _parameters, ProjectName, gitCol, _changedGitProjects, true,
            true);
    }

    private void AssertErrorLine(string expected)
    {
        Assert.Contains($"[ERROR] {expected}{Environment.NewLine}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
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
}
