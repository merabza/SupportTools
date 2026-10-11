using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGitData.Models;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//D3: the checks of "Clone Project To This Computer" read only the parameters and the files, and write nothing to
//the console (the command writes the report)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectCloneChecksTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), $"ProjectCloneChecksTests_{Guid.NewGuid():N}");

    public ProjectCloneChecksTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
        Directory.CreateDirectory(_tempFolder);
        _parameters.Gits["RepoA"] = CreateGit("RepoA", "CSharp");
        _parameters.Gits["RepoB"] = CreateGit("RepoB", "CSharp");
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    //a git without an address is left out silently
    [Fact]
    public void GetGitFolders_WhenAGitHasNoAddress_WritesNothing()
    {
        // Arrange
        _parameters.Gits["NoAddress"] = new GitDataModel { GitProjectFolderName = "NoAddress", GitIgnorePatternName = "CSharp" };

        // Act
        Dictionary<string, string?> result = ProjectCloneChecks.GetGitFolders(_parameters, "P",
            new ProjectModel { GitProjectNames = ["RepoA", "NoAddress"] }, _tempFolder, ["RepoA", "NoAddress"]);

        // Assert
        Assert.Null(result["NoAddress"]);
        Assert.Empty(_consoleOutput.ToString());
    }

    //a git that is not in Gits or has no pattern has no template to check
    [Fact]
    public void GetTemplateProblems_WhenGitIsUnknownOrHasNoPattern_SkipsIt()
    {
        // Arrange
        WriteTemplate("CSharp.gitignore", "bin/");
        _parameters.Gits["NoPattern"] = CreateGit("NoPattern", " ");

        // Act
        List<string> result = ProjectCloneChecks.GetTemplateProblems(_parameters, new ProjectModel(),
            new Dictionary<string, string?> { ["Unknown"] = _tempFolder, ["NoPattern"] = _tempFolder });

        // Assert
        Assert.Empty(result);
    }

    //only a missing template is reported for a git, its .gitignore is not compared then
    [Fact]
    public void GetTemplateProblems_WhenTemplateIsMissingAndTheFileExists_ReportsOnlyTheTemplate()
    {
        // Arrange
        _parameters.FolderForGitignoreFiles = _tempFolder;
        string gitFolder = CreateGitFolder("RepoA", "bin/");

        // Act
        List<string> result = ProjectCloneChecks.GetTemplateProblems(_parameters, new ProjectModel(),
            new Dictionary<string, string?> { ["RepoA"] = gitFolder });

        // Assert
        Assert.Equal([$"The .gitignore template CSharp is missing: {Path.Combine(_tempFolder, "CSharp.gitignore")}"],
            result);
    }

    [Fact]
    public void GetTemplateProblems_WhenTemplateExistsAndTheGitHasNoFolder_ReportsNothing()
    {
        // Arrange
        WriteTemplate("CSharp.gitignore", "bin/");

        // Act
        List<string> result = ProjectCloneChecks.GetTemplateProblems(_parameters, new ProjectModel(),
            new Dictionary<string, string?> { ["RepoA"] = null });

        // Assert
        Assert.Empty(result);
    }

    //an .editorconfig is checked only for a project with both a pattern and a solution file
    [Theory]
    [InlineData("default", null)]
    [InlineData(null, "App.slnx")]
    [InlineData(" ", "App.slnx")]
    public void GetTemplateProblems_WhenProjectHasNoPatternOrNoSolution_DoesNotCheckTheEditorConfig(
        string? editorConfigPatternName, string? solutionFileName)
    {
        // Arrange
        _parameters.FolderForEditorConfigFiles = _tempFolder;
        var project = new ProjectModel
        {
            EditorConfigPatternName = editorConfigPatternName,
            SolutionFileName = solutionFileName is null ? null : Path.Combine(_tempFolder, solutionFileName)
        };

        // Act
        List<string> result =
            ProjectCloneChecks.GetTemplateProblems(_parameters, project, new Dictionary<string, string?>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetGitsWithoutGitProjects_WhenCalled_ReturnsKnownGitsWithoutRecordsIgnoringCase()
    {
        // Arrange
        _parameters.GitProjects["LibA"] = new GitProjectDataModel { GitName = "repoa" };

        // Act
        List<string> result =
            ProjectCloneChecks.GetGitsWithoutGitProjects(_parameters, ["RepoA", "RepoB", "Unknown", "RepoB"]);

        // Assert
        Assert.Equal(["RepoB"], result);
    }

    [Fact]
    public void GetGitFolders_WhenGitsFolderIsKnown_CombinesItWithTheGitFolderName()
    {
        // Arrange
        var project = new ProjectModel { GitProjectNames = ["RepoA"] };

        // Act
        Dictionary<string, string?> result =
            ProjectCloneChecks.GetGitFolders(_parameters, "P", project, _tempFolder, ["RepoA", "Unknown"]);

        // Assert
        Assert.Equal(Path.Combine(_tempFolder, "RepoA"), result["RepoA"]);
        Assert.Null(result["Unknown"]);
    }

    [Fact]
    public void GetGitFolders_WhenGitsFolderIsUnknown_ReturnsNull()
    {
        // Act
        Dictionary<string, string?> result =
            ProjectCloneChecks.GetGitFolders(_parameters, "P", new ProjectModel(), null, ["RepoA"]);

        // Assert
        Assert.Null(result["RepoA"]);
    }

    [Fact]
    public void GetTemplateProblems_WhenFilesMatchTheTemplates_ReturnsNothing()
    {
        // Arrange
        string gitFolder = CreateGitFolder("RepoA", "bin/");
        WriteTemplate("CSharp.gitignore", "bin/");

        // Act
        List<string> result = ProjectCloneChecks.GetTemplateProblems(_parameters, new ProjectModel(),
            new Dictionary<string, string?> { ["RepoA"] = gitFolder });

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetTemplateProblems_WhenTemplateIsMissing_ReportsItAlsoForAGitWithoutFolder()
    {
        // Arrange
        _parameters.FolderForGitignoreFiles = _tempFolder;
        string templateFileName = Path.Combine(_tempFolder, "CSharp.gitignore");

        // Act
        List<string> result = ProjectCloneChecks.GetTemplateProblems(_parameters, new ProjectModel(),
            new Dictionary<string, string?> { ["RepoA"] = null });

        // Assert
        Assert.Equal([$"The .gitignore template CSharp is missing: {templateFileName}"], result);
    }

    [Fact]
    public void GetTemplateProblems_WhenGitignoreDiffersOrIsMissing_ReportsEachGit()
    {
        // Arrange
        string folderA = CreateGitFolder("RepoA", "obj/");
        string folderB = Path.Combine(_tempFolder, "RepoB");
        Directory.CreateDirectory(folderB);
        WriteTemplate("CSharp.gitignore", "bin/");

        // Act
        List<string> result = ProjectCloneChecks.GetTemplateProblems(_parameters, new ProjectModel(),
            new Dictionary<string, string?> { ["RepoB"] = folderB, ["RepoA"] = folderA });

        // Assert
        Assert.Equal(
        [
            $"{Path.Combine(folderA, ".gitignore")} differs from the .gitignore template CSharp",
            $"{Path.Combine(folderB, ".gitignore")} does not exist (.gitignore template CSharp)"
        ], result);
    }

    [Fact]
    public void GetTemplateProblems_WhenGitignoreFolderIsNotSpecified_ReportsItOnce()
    {
        // Act
        List<string> result = ProjectCloneChecks.GetTemplateProblems(_parameters, new ProjectModel(),
            new Dictionary<string, string?> { ["RepoA"] = null, ["RepoB"] = null });

        // Assert
        Assert.Equal(["FolderForGitignoreFiles is not specified, the .gitignore files are not checked"], result);
    }

    [Fact]
    public void GetTemplateProblems_WhenEditorConfigDiffersFromTheTemplate_ReportsIt()
    {
        // Arrange
        _parameters.FolderForEditorConfigFiles = _tempFolder;
        File.WriteAllText(Path.Combine(_tempFolder, "default.editorconfig"), "root = true");
        string solutionFolder = Path.Combine(_tempFolder, "Solution");
        Directory.CreateDirectory(solutionFolder);
        string editorConfig = Path.Combine(solutionFolder, ".editorconfig");
        File.WriteAllText(editorConfig, "root = false");
        var project = new ProjectModel
        {
            EditorConfigPatternName = "default", SolutionFileName = Path.Combine(solutionFolder, "App.slnx")
        };

        // Act
        List<string> result =
            ProjectCloneChecks.GetTemplateProblems(_parameters, project, new Dictionary<string, string?>());

        // Assert
        Assert.Equal([$"{editorConfig} differs from the .editorconfig template default"], result);
    }

    [Fact]
    public void GetTemplateProblems_WhenEditorConfigFolderIsNotSpecified_ReportsIt()
    {
        // Arrange
        var project = new ProjectModel
        {
            EditorConfigPatternName = "default", SolutionFileName = Path.Combine(_tempFolder, "App.slnx")
        };

        // Act
        List<string> result =
            ProjectCloneChecks.GetTemplateProblems(_parameters, project, new Dictionary<string, string?>());

        // Assert
        Assert.Equal(["FolderForEditorConfigFiles is not specified, the .editorconfig file is not checked"], result);
    }

    [Fact]
    public void GetMissingStoredFiles_WhenCalled_ReturnsMissingFilesOnceInFieldOrder()
    {
        // Arrange
        string existing = Path.Combine(_tempFolder, "existing.json");
        File.WriteAllText(existing, "{}");
        string appSettings = Path.Combine(_tempFolder, "appsettings.json");
        string paired = Path.Combine(_tempFolder, "paired.json");
        var project = new ProjectModel
        {
            SeedProjectParametersFilePath = existing,
            PrepareProdCopyDatabaseProjectParametersFilePath = " ",
            PairedDbObjectsResultFileName = paired
        };
        project.ServerInfos["A"] = new ServerInfoModel { AppSettingsJsonSourceFileName = appSettings };
        project.ServerInfos["B"] = new ServerInfoModel { AppSettingsJsonSourceFileName = appSettings.ToUpperInvariant() };
        project.ServerInfos["C"] = new ServerInfoModel();

        // Act
        List<string> result = ProjectCloneChecks.GetMissingStoredFiles(project);

        // Assert
        Assert.Equal([paired, appSettings], result);
    }

    private static GitDataModel CreateGit(string gitName, string gitIgnorePatternName)
    {
        return new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{gitName}.git",
            GitProjectFolderName = gitName,
            GitIgnorePatternName = gitIgnorePatternName
        };
    }

    private string CreateGitFolder(string gitName, string gitignoreContent)
    {
        string gitFolder = Path.Combine(_tempFolder, gitName);
        Directory.CreateDirectory(gitFolder);
        File.WriteAllText(Path.Combine(gitFolder, ".gitignore"), gitignoreContent);
        return gitFolder;
    }

    private void WriteTemplate(string fileName, string content)
    {
        _parameters.FolderForGitignoreFiles = _tempFolder;
        File.WriteAllText(Path.Combine(_tempFolder, fileName), content);
    }
}
