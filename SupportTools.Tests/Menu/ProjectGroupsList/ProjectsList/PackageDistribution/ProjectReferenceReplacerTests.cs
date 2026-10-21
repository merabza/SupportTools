using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

//dotnet add package restores through the NuGet HTTP cache and misses a dependency uploaded a moment ago, so the package
//is added without a restore and the project is restored without the cache; every failure puts the reference back
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectReferenceReplacerTests : IDisposable
{
    private const string Project = @"C:\Repo\App\App.csproj";
    private const string Reference = @"C:\Repo\Lib\Lib.A\Lib.A.csproj";

    private readonly FakePackageReferenceCommands _commands = new();
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly ProjectReferenceReplacer _sut;

    public ProjectReferenceReplacerTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
        _sut = new ProjectReferenceReplacer(_commands, null);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Replace_WhenEveryCommandSucceeds_AddsThePackageWithoutRestoreThenRestoresWithoutHttpCache()
    {
        // Act
        bool result = _sut.Replace(Project, Reference, "Lib.A", "1.0.2");

        // Assert
        Assert.True(result);
        Assert.Equal(
        [
            $"remove reference {Project} {Reference}", $"add package without restore {Project} Lib.A 1.0.2",
            $"restore without http cache {Project}"
        ], _commands.Calls);
        Assert.DoesNotContain("[ERROR]", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Replace_WhenTheReferenceCannotBeRemoved_ChangesNothingElse()
    {
        // Arrange
        _commands.Failing.Add("remove reference");

        // Act
        bool result = _sut.Replace(Project, Reference, "Lib.A", "1.0.2");

        // Assert
        Assert.False(result);
        Assert.Equal([$"remove reference {Project} {Reference}"], _commands.Calls);
        Assert.Contains($"Cannot remove reference {Reference} from {Project}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Replace_WhenThePackageCannotBeAdded_RestoresTheReference()
    {
        // Arrange
        _commands.Failing.Add("add package without restore");

        // Act
        bool result = _sut.Replace(Project, Reference, "Lib.A", "1.0.2");

        // Assert
        Assert.False(result);
        Assert.Equal(
        [
            $"remove reference {Project} {Reference}", $"add package without restore {Project} Lib.A 1.0.2",
            $"add reference {Project} {Reference}"
        ], _commands.Calls);
        Assert.Contains($"Cannot add package Lib.A to {Project}. Removed reference was restored",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Replace_WhenThePackageCannotBeAddedAndTheReferenceCannotBeRestored_AsksToRecoverFromGit()
    {
        // Arrange
        _commands.Failing.Add("add package without restore");
        _commands.Failing.Add("add reference");

        // Act
        bool result = _sut.Replace(Project, Reference, "Lib.A", "1.0.2");

        // Assert
        Assert.False(result);
        Assert.Contains(
            $"Cannot add package Lib.A to {Project} and cannot restore removed reference {Reference}. Recover the file from git",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Replace_WhenTheRestoreFails_RemovesThePackageAndRestoresTheReference()
    {
        // Arrange
        _commands.Failing.Add("restore without http cache");

        // Act
        bool result = _sut.Replace(Project, Reference, "Lib.A", "1.0.2");

        // Assert
        Assert.False(result);
        Assert.Equal(
        [
            $"remove reference {Project} {Reference}", $"add package without restore {Project} Lib.A 1.0.2",
            $"restore without http cache {Project}", $"remove package {Project} Lib.A",
            $"add reference {Project} {Reference}"
        ], _commands.Calls);
        Assert.Contains($"Cannot restore {Project} with package Lib.A 1.0.2. Removed reference was restored",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Replace_WhenTheRestoreFailsAndTheReferenceCannotBeRestored_AsksToRecoverFromGit()
    {
        // Arrange
        _commands.Failing.Add("restore without http cache");
        _commands.Failing.Add("add reference");

        // Act
        bool result = _sut.Replace(Project, Reference, "Lib.A", "1.0.2");

        // Assert
        Assert.False(result);
        Assert.Contains(
            $"Cannot restore {Project} with package Lib.A 1.0.2 and cannot restore removed reference {Reference}. Recover the file from git",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Replace_WhenTheRestoreFailsAndThePackageCannotBeRemoved_LeavesTheReferenceOutAndAsksToRecoverFromGit()
    {
        // Arrange
        _commands.Failing.Add("restore without http cache");
        _commands.Failing.Add("remove package");

        // Act
        bool result = _sut.Replace(Project, Reference, "Lib.A", "1.0.2");

        // Assert
        Assert.False(result);
        Assert.Equal(
        [
            $"remove reference {Project} {Reference}", $"add package without restore {Project} Lib.A 1.0.2",
            $"restore without http cache {Project}", $"remove package {Project} Lib.A"
        ], _commands.Calls);
        Assert.Contains(
            $"Cannot restore {Project} with package Lib.A 1.0.2 and cannot remove the package and restore removed reference {Reference}. Recover the file from git",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private sealed class FakePackageReferenceCommands : IPackageReferenceCommands
    {
        public List<string> Calls { get; } = [];
        public HashSet<string> Failing { get; } = [];

        public Result RemoveReferenceFromProject(string projectFilePath, string referenceProjectFilePath)
        {
            return Run("remove reference", $"{projectFilePath} {referenceProjectFilePath}");
        }

        public Result AddPackageToProjectWithoutRestore(string projectFilePath, string packageName,
            string packageVersion)
        {
            return Run("add package without restore", $"{projectFilePath} {packageName} {packageVersion}");
        }

        public Result RestoreWithoutHttpCache(string projectFilePath)
        {
            return Run("restore without http cache", projectFilePath);
        }

        public Result RemovePackageFromProject(string projectFilePath, string packageName)
        {
            return Run("remove package", $"{projectFilePath} {packageName}");
        }

        public Result AddReferenceToProject(string projectFilePath, string referenceProjectFilePath)
        {
            return Run("add reference", $"{projectFilePath} {referenceProjectFilePath}");
        }

        private Result Run(string command, string arguments)
        {
            Calls.Add($"{command} {arguments}");
            return Failing.Contains(command) ? Result.Failure(Error.Failure("Failed", "failed")) : Result.Success();
        }
    }
}
