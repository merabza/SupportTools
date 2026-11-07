using System;
using System.Diagnostics;
using System.IO;
using LibGitWork.OutdatedPackages;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.OutdatedPackages;

public sealed class DotnetPackageUpdaterCommandsTests
{
    //the same properties as the restores of dotnet outdated's analysis, so that they become no-ops
    [Fact]
    public void CreateRestoreForOutdatedArguments_UsesThePropertiesOfDotnetOutdated()
    {
        // Act
        string[] result = DotnetPackageUpdaterCommands.CreateRestoreForOutdatedArguments(@"C:\Work\GitA\GitA.slnx");

        // Assert
        Assert.Equal(
            ["restore", @"C:\Work\GitA\GitA.slnx", "-p:NoWarn=NU1605", "-p:TreatWarningsAsErrors=false"], result);
    }

    //-n: dotnet outdated writes the versions into Directory.Packages.props itself, without one dotnet add package (with
    //a restore preview and a restore) per project and package
    [Fact]
    public void CreateUpdateOutdatedPackagesArguments_UpgradesWithoutARestorePerPackage()
    {
        // Act
        string[] result = DotnetPackageUpdaterCommands.CreateUpdateOutdatedPackagesArguments(@"C:\Work\GitA");

        // Assert
        Assert.Equal(["outdated", "-r", "-u", "-n", @"C:\Work\GitA"], result);
    }

    //a plain restore with the projects' own properties, as a build does
    [Fact]
    public void CreateRestoreAfterOutdatedArguments_RestoresLikeABuild()
    {
        // Act
        string[] result = DotnetPackageUpdaterCommands.CreateRestoreAfterOutdatedArguments(@"C:\Work\GitA\GitA.slnx");

        // Assert
        Assert.Equal(["restore", @"C:\Work\GitA\GitA.slnx"], result);
    }

    //the output is kept, never written to the console; a path with a space stays one argument. A reused MSBuild node
    //would keep the output pipe open after the command, and the wait for its end would hang
    [Fact]
    public void CreateStartInfo_WhenCalled_CapturesTheOutputAndKeepsEveryArgumentWhole()
    {
        // Act
        ProcessStartInfo result =
            DotnetPackageUpdaterCommands.CreateStartInfo(["restore", @"C:\My Work\GitA\GitA.slnx"]);

        // Assert
        Assert.Equal("dotnet", result.FileName);
        Assert.Equal(["restore", @"C:\My Work\GitA\GitA.slnx"], result.ArgumentList);
        Assert.False(result.UseShellExecute);
        Assert.True(result.RedirectStandardInput);
        Assert.True(result.RedirectStandardOutput);
        Assert.True(result.RedirectStandardError);
        Assert.True(result.CreateNoWindow);
        Assert.Equal("1", result.Environment["MSBUILDDISABLENODEREUSE"]);
    }

    [Fact]
    public void RestoreForOutdated_WhenTheSolutionIsMissing_ReturnsTheCommandAndTheOutputOfDotnetInTheError()
    {
        // Arrange
        string solutionFile = Path.Combine(Path.GetTempPath(), $"SupportToolsTests_{Guid.NewGuid():N}", "Missing.slnx");

        // Act
        Result result = new DotnetPackageUpdaterCommands().RestoreForOutdated(solutionFile);

        // Assert
        Assert.True(result.IsFailure);
        Assert.StartsWith(
            $"dotnet restore {solutionFile} -p:NoWarn=NU1605 -p:TreatWarningsAsErrors=false failed (ExitCode=1)",
            result.Error.Description, StringComparison.Ordinal);
        Assert.Contains("MSB1009", result.Error.Description, StringComparison.Ordinal);
    }

    //dotnet outdated (the global tool the command needs) writes why to stderr, and stderr is kept too
    [Fact]
    public void UpdateOutdatedPackages_WhenTheFolderIsMissing_ReturnsTheCommandAndItsErrorOutputInTheError()
    {
        // Arrange
        string folder = Path.Combine(Path.GetTempPath(), $"SupportToolsTests_{Guid.NewGuid():N}");

        // Act
        Result result = new DotnetPackageUpdaterCommands().UpdateOutdatedPackages(folder);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("DotnetCommandFailed", result.Error.Code);
        Assert.StartsWith($"dotnet outdated -r -u -n {folder} failed (ExitCode=1)", result.Error.Description,
            StringComparison.Ordinal);
        Assert.Contains($"'{folder}' does not exist", result.Error.Description, StringComparison.Ordinal);
    }

    //a solution without projects has nothing to restore, and the real restore succeeds
    [Fact]
    public void RestoreAfterOutdated_WhenTheSolutionHasNoProjects_Succeeds()
    {
        // Arrange
        string folder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        string solutionFile = Path.Combine(folder, "Empty.slnx");
        File.WriteAllText(solutionFile, "<Solution />");

        try
        {
            // Act
            Result result = new DotnetPackageUpdaterCommands().RestoreAfterOutdated(solutionFile);

            // Assert
            Assert.True(result.IsSuccess);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    //the real dotnet: its error (written to stdout) reaches the error text, with the command and its exit code
    [Fact]
    public void RestoreAfterOutdated_WhenTheSolutionIsMissing_ReturnsTheOutputOfDotnetInTheError()
    {
        // Arrange
        string solutionFile = Path.Combine(Path.GetTempPath(), $"SupportToolsTests_{Guid.NewGuid():N}", "Missing.slnx");

        // Act
        Result result = new DotnetPackageUpdaterCommands().RestoreAfterOutdated(solutionFile);

        // Assert
        Assert.True(result.IsFailure);
        Assert.StartsWith($"dotnet restore {solutionFile} failed (ExitCode=1)", result.Error.Description,
            StringComparison.Ordinal);
        Assert.Contains("MSB1009", result.Error.Description, StringComparison.Ordinal);
    }
}
