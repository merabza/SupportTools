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
