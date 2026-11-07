using System;
using System.IO;
using System.Text;
using LibGitWork;
using LibGitWork.ToolActions;
using SupportTools.Tests.OutdatedPackages;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//no restore and no dotnet outdated run: the commands are FakePackageUpdaterCommands, the steps and lines are recorded
//by FakePackageUpdateOutput. The git folder GitA holds GitA.slnx and a Directory.Packages.props with a BOM; a dotnet
//outdated that writes versions rewrites the file without the BOM, as the real one does with -n
public sealed class PackageUpdaterToolActionTests : IDisposable
{
    private const string FakeError = "the fake command failed";

    private const string OldText =
        "<Project>\r\n  <ItemGroup>\r\n    <PackageVersion Include=\"Polly\" Version=\"8.8.0\" />\r\n  </ItemGroup>\r\n</Project>\r\n";

    private const string NewText =
        "<Project>\r\n  <ItemGroup>\r\n    <PackageVersion Include=\"Polly\" Version=\"8.9.0\" />\r\n  </ItemGroup>\r\n</Project>\r\n";

    private readonly FakePackageUpdaterCommands _commands = new();
    private readonly string _gitFolder;
    private readonly FakePackageUpdateOutput _output = new();
    private readonly string _propsFile;
    private readonly string _solutionFile;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public PackageUpdaterToolActionTests()
    {
        _gitFolder = Path.Combine(_tempFolder, "GitA");
        Directory.CreateDirectory(_gitFolder);
        _solutionFile = Path.Combine(_gitFolder, "GitA.slnx");
        File.WriteAllText(_solutionFile, "<Solution />");
        _propsFile = Path.Combine(_gitFolder, "Directory.Packages.props");
        File.WriteAllBytes(_propsFile, WithBom(OldText));
    }

    public void Dispose()
    {
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void RunPackageUpdate_WhenTheGitIsNotACSharpGit_RunsNothing()
    {
        // Act
        EPackageUpdateResult result = CreateSut("Node").RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Empty(_commands.Commands);
        Assert.Empty(_output.Statuses);
    }

    //nothing was written, so there is nothing to check and nothing to report
    [Fact]
    public void RunPackageUpdate_WhenNothingIsOutdated_DoesNotRestoreAgain()
    {
        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA"], _commands.Commands);
        Assert.Equal(["restore", "dotnet outdated"], _output.Statuses);
        Assert.Empty(_output.Lines);
        Assert.Equal(WithBom(OldText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenVersionsAreWritten_PutsTheBomBackAndRestoresTheSolution()
    {
        // Arrange
        _commands.OnOutdated = () => WriteProps(NewText);

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA", "restore after outdated GitA.slnx"],
            _commands.Commands);
        Assert.Equal(["restore", "dotnet outdated", "check restore"], _output.Statuses);
        Assert.Equal(("1 package version updated", (ConsoleColor?)null), Assert.Single(_output.Lines));
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenSeveralVersionsAreWritten_CountsThem()
    {
        // Arrange
        const string versionsBefore =
            "<PackageVersion Include=\"A\" Version=\"1.0.0\" />\r\n" +
            "<PackageVersion Include=\"B\" Version=\"2.0.0\" />\r\n";
        File.WriteAllBytes(_propsFile, WithBom(versionsBefore));
        _commands.OnOutdated = () => WriteProps(versionsBefore.Replace("1.0.0", "1.0.1", StringComparison.Ordinal)
            .Replace("2.0.0", "2.1.0", StringComparison.Ordinal));

        // Act
        CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(("2 package versions updated", (ConsoleColor?)null), Assert.Single(_output.Lines));
    }

    [Fact]
    public void RunPackageUpdate_WhenTheRestoreAfterTheUpdateFails_ReportsItInRed()
    {
        // Arrange
        _commands.OnOutdated = () => WriteProps(NewText);
        _commands.RestoreAfterOutdatedFails = true;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.RestoreAfterUpdateFailed, result);
        (string text, ConsoleColor? color) = _output.Lines[^1];
        Assert.Equal($"restore after the update failed for {_solutionFile}{Environment.NewLine}{FakeError}", text);
        Assert.Equal(ConsoleColor.Red, color);
    }

    //the attempts before the last one are warnings; the versions written before the failure get the BOM back and are
    //not checked
    [Fact]
    public void RunPackageUpdate_WhenDotnetOutdatedFailsEveryTime_ReportsTheLastAttemptInRed()
    {
        // Arrange
        _commands.OnOutdated = () => WriteProps(NewText);
        _commands.OutdatedFailures = 3;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.OutdatedFailed, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA", "outdated GitA", "outdated GitA"],
            _commands.Commands);
        Assert.Equal(
            ["restore", "dotnet outdated", "dotnet outdated, attempt 2 of 3", "dotnet outdated, attempt 3 of 3"],
            _output.Statuses);
        Assert.Equal<ConsoleColor?>([ConsoleColor.Yellow, ConsoleColor.Yellow, ConsoleColor.Red],
            _output.Lines.ConvertAll(x => x.Color));
        Assert.Equal($"dotnet outdated failed for {_gitFolder} (attempt 3 of 3){Environment.NewLine}{FakeError}",
            _output.Lines[^1].Text);
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenDotnetOutdatedFailsOnce_TriesAgain()
    {
        // Arrange
        _commands.OutdatedFailures = 1;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA", "outdated GitA"], _commands.Commands);
        (string text, ConsoleColor? color) = Assert.Single(_output.Lines);
        Assert.StartsWith($"dotnet outdated failed for {_gitFolder} (attempt 1 of 3)", text, StringComparison.Ordinal);
        Assert.Equal(ConsoleColor.Yellow, color);
    }

    [Fact]
    public void RunPackageUpdate_WhenTheRestoreBeforeOutdatedFails_WarnsAndGoesOn()
    {
        // Arrange
        _commands.RestoreForOutdatedFails = true;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA"], _commands.Commands);
        (string text, ConsoleColor? color) = Assert.Single(_output.Lines);
        Assert.Equal(
            $"restore before dotnet outdated failed for {_solutionFile}, continuing{Environment.NewLine}{FakeError}",
            text);
        Assert.Equal(ConsoleColor.Yellow, color);
    }

    //without a single solution there is nothing to restore, before or after the update
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void RunPackageUpdate_WhenTheFolderHasNoSingleSolution_RestoresNothing(int solutionCount)
    {
        // Arrange
        File.Delete(_solutionFile);
        for (int i = 0; i < solutionCount; i++)
        {
            File.WriteAllText(Path.Combine(_gitFolder, $"GitA{i}.sln"), string.Empty);
        }

        _commands.OnOutdated = () => WriteProps(NewText);

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["outdated GitA"], _commands.Commands);
        Assert.Equal(["dotnet outdated"], _output.Statuses);
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenTheSolutionIsAnSlnFile_RestoresIt()
    {
        // Arrange
        File.Move(_solutionFile, Path.Combine(_gitFolder, "GitA.sln"));

        // Act
        CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(["restore for outdated GitA.sln", "outdated GitA"], _commands.Commands);
    }

    //the extension of a solution is compared without case
    [Fact]
    public void RunPackageUpdate_WhenTheSolutionExtensionIsInUpperCase_RestoresIt()
    {
        // Arrange
        File.Move(_solutionFile, Path.Combine(_gitFolder, "GitA.SLNX"));

        // Act
        CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(["restore for outdated GitA.SLNX", "outdated GitA"], _commands.Commands);
    }

    [Fact]
    public void RunPackageUpdate_WhenTheFolderIsMissing_RunsOnlyDotnetOutdated()
    {
        // Arrange
        Directory.Delete(_gitFolder, true);

        // Act
        CreateSut().RunPackageUpdate(_output);

        // Assert
        Assert.Equal(["outdated GitA"], _commands.Commands);
    }

    private PackageUpdaterToolAction CreateSut(string gitIgnorePathName = PackageUpdaterToolAction.CSharp)
    {
        return new PackageUpdaterToolAction(null, _gitFolder, gitIgnorePathName, _commands);
    }

    //as dotnet outdated -n does: File.WriteAllText keeps the line breaks of the text and writes no BOM
    private void WriteProps(string text)
    {
        File.WriteAllText(_propsFile, text);
    }

    private static byte[] WithBom(string text)
    {
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)];
    }
}
