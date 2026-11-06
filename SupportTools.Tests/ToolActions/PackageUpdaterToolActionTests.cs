using System;
using System.Globalization;
using System.IO;
using System.Text;
using LibGitWork;
using LibGitWork.ToolActions;
using SupportTools.Tests.OutdatedPackages;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//no restore and no dotnet outdated run: the commands are FakePackageUpdaterCommands. The git folder GitA holds GitA.slnx
//and a Directory.Packages.props with a BOM; a dotnet outdated that writes versions rewrites the file without the BOM, as
//the real one does with -n
[Collection(ConsoleCaptureCollection.Name)]
public sealed class PackageUpdaterToolActionTests : IDisposable
{
    private const string OldText =
        "<Project>\r\n  <ItemGroup>\r\n    <PackageVersion Include=\"Polly\" Version=\"8.8.0\" />\r\n  </ItemGroup>\r\n</Project>\r\n";

    private const string NewText =
        "<Project>\r\n  <ItemGroup>\r\n    <PackageVersion Include=\"Polly\" Version=\"8.9.0\" />\r\n  </ItemGroup>\r\n</Project>\r\n";

    private readonly FakePackageUpdaterCommands _commands = new();
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly string _gitFolder;
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _propsFile;
    private readonly string _solutionFile;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public PackageUpdaterToolActionTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _gitFolder = Path.Combine(_tempFolder, "GitA");
        Directory.CreateDirectory(_gitFolder);
        _solutionFile = Path.Combine(_gitFolder, "GitA.slnx");
        File.WriteAllText(_solutionFile, "<Solution />");
        _propsFile = Path.Combine(_gitFolder, "Directory.Packages.props");
        File.WriteAllBytes(_propsFile, WithBom(OldText));
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void RunPackageUpdate_WhenTheGitIsNotACSharpGit_RunsNothing()
    {
        // Act
        EPackageUpdateResult result = CreateSut("Node").RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Empty(_commands.Commands);
    }

    //nothing was written, so there is nothing to check
    [Fact]
    public void RunPackageUpdate_WhenNothingIsOutdated_DoesNotRestoreAgain()
    {
        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA"], _commands.Commands);
        Assert.Equal(WithBom(OldText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenVersionsAreWritten_PutsTheBomBackAndRestoresTheSolution()
    {
        // Arrange
        _commands.OnOutdated = WriteNewVersions;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA", "restore after outdated GitA.slnx"],
            _commands.Commands);
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenTheRestoreAfterTheUpdateFails_ReportsIt()
    {
        // Arrange
        _commands.OnOutdated = WriteNewVersions;
        _commands.RestoreAfterOutdatedFails = true;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.RestoreAfterUpdateFailed, result);
        Assert.Contains($"restore after the update failed for {_solutionFile}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //the versions written before the failure get the BOM back and are not checked
    [Fact]
    public void RunPackageUpdate_WhenDotnetOutdatedFailsEveryTime_ReportsItWithoutTheCheck()
    {
        // Arrange
        _commands.OnOutdated = WriteNewVersions;
        _commands.OutdatedFailures = 3;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.OutdatedFailed, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA", "outdated GitA", "outdated GitA"],
            _commands.Commands);
        Assert.Contains("(attempt 3 of 3)", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenDotnetOutdatedFailsOnce_TriesAgain()
    {
        // Arrange
        _commands.OutdatedFailures = 1;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA", "outdated GitA"], _commands.Commands);
        Assert.Contains("(attempt 1 of 3)", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RunPackageUpdate_WhenTheRestoreBeforeOutdatedFails_GoesOn()
    {
        // Arrange
        _commands.RestoreForOutdatedFails = true;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["restore for outdated GitA.slnx", "outdated GitA"], _commands.Commands);
        Assert.Contains($"restore before dotnet outdated failed for {_solutionFile}, continuing",
            _consoleOutput.ToString(), StringComparison.Ordinal);
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

        _commands.OnOutdated = WriteNewVersions;

        // Act
        EPackageUpdateResult result = CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(EPackageUpdateResult.Success, result);
        Assert.Equal(["outdated GitA"], _commands.Commands);
        Assert.Equal(WithBom(NewText), File.ReadAllBytes(_propsFile));
    }

    [Fact]
    public void RunPackageUpdate_WhenTheFolderIsMissing_RunsOnlyDotnetOutdated()
    {
        // Arrange
        Directory.Delete(_gitFolder, true);

        // Act
        CreateSut().RunPackageUpdate();

        // Assert
        Assert.Equal(["outdated GitA"], _commands.Commands);
    }

    private PackageUpdaterToolAction CreateSut(string gitIgnorePathName = PackageUpdaterToolAction.CSharp)
    {
        return new PackageUpdaterToolAction(null, _gitFolder, gitIgnorePathName, _commands);
    }

    //as dotnet outdated -n does: File.WriteAllText keeps the line breaks of the text and writes no BOM
    private void WriteNewVersions()
    {
        File.WriteAllText(_propsFile,
            File.ReadAllText(_propsFile).Replace("8.8.0", "8.9.0", StringComparison.Ordinal));
    }

    private static byte[] WithBom(string text)
    {
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)];
    }
}
