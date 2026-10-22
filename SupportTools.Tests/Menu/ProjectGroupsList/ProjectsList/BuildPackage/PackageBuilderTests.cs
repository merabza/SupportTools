using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.BuildPackage;

//package project Pkg: its solution {temp}\Pkg\Pkg\Pkg.slnx has its own projects Pkg.A, Pkg.B and Pkg.Tests and, as the
//package solutions list ../SystemTools/..., project Other.Lib of the clone of another repository next to it. The pack is
//a fake that writes the packages such a solution gives, the upload a fake that notes what it would send
[Collection(ConsoleCaptureCollection.Name)]
public sealed class PackageBuilderTests : IDisposable
{
    private const string ApiClientName = "LocalPackages";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _outputFolder;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    //the output folder of the builder is named after the project, so every test has a project of its own
    private readonly string _projectName = $"PackageBuilderTests{Guid.NewGuid():N}";
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public PackageBuilderTests()
    {
        string solutionFolder = Path.Combine(_tempFolder, "Pkg", "Pkg");
        WriteProjectFile(solutionFolder, "Pkg.A");
        WriteProjectFile(solutionFolder, "Pkg.B");
        WriteProjectFile(solutionFolder, "Pkg.Tests");
        WriteProjectFile(Path.Combine(_tempFolder, "Pkg", "Other"), "Other.Lib");

        _parameters.LocalPackageManagerWebApiClientName = ApiClientName;
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/", ApiKey = "key" };
        _parameters.Projects[_projectName] = new ProjectModel
        {
            ProjectType = EProjectType.IsPackage, SolutionFileName = Path.Combine(solutionFolder, "Pkg.slnx")
        };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _outputFolder = Path.Combine(Path.GetTempPath(), "SupportTools", "BuildPackage", _projectName);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
        if (Directory.Exists(_outputFolder))
        {
            Directory.Delete(_outputFolder, true);
        }
    }

    //uploaded, the copy of Other.Lib would be the latest Other.Lib on the server, which the distribution takes, and it
    //asks for Other.Base of the version of Pkg, which is never uploaded (NU1102). A package of another repository that
    //an own package uses with its own version is no reason to stop
    [Fact]
    public void BuildAndUploadPackage_WhenTheSolutionPacksAProjectOfAnotherRepository_UploadsOnlyItsOwnPackages()
    {
        // Arrange
        var commands = new FakePackageBuildCommands(new FakePackage("Pkg.A", ("Pkg.B", null), ("Other.Lib", "1.0.5")),
            new FakePackage("Pkg.B"), new FakePackage("Other.Lib", ("Other.Base", null)));

        // Act
        bool result = CreateSut(commands).BuildAndUploadPackage();

        // Assert
        Assert.True(result);
        Assert.Equal([$"{Path.Combine(_outputFolder, "*.nupkg")} http://127.0.0.1:0/v3/index.json key"],
            commands.Pushes);
        Assert.Equal([$"Pkg.A.{commands.PackageVersion}.nupkg", $"Pkg.B.{commands.PackageVersion}.nupkg"],
            commands.FilesAtPush);
        Assert.Contains("Packages of other repositories are not uploaded: Other.Lib", ConsoleText(),
            StringComparison.Ordinal);
    }

    //Pkg.A still references project Other.Lib, so its package asks for Other.Lib of the version of Pkg, which only the
    //copy that is not uploaded has
    [Fact]
    public void BuildAndUploadPackage_WhenAnOwnPackageReferencesAProjectOfAnotherRepository_UploadsNothing()
    {
        // Arrange
        var commands = new FakePackageBuildCommands(new FakePackage("Pkg.A", ("Other.Lib", null)),
            new FakePackage("Pkg.B"), new FakePackage("Other.Lib"));

        // Act
        bool result = CreateSut(commands).BuildAndUploadPackage();

        // Assert
        Assert.False(result);
        Assert.Empty(commands.Pushes);
        Assert.Contains(
            $"Packages of project {_projectName} depend on packages of version {commands.PackageVersion} that are not uploaded: Pkg.A -> Other.Lib",
            ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAndUploadPackage_WhenOnlyPackagesOfOtherRepositoriesAreCreated_UploadsNothing()
    {
        // Arrange
        var commands = new FakePackageBuildCommands(new FakePackage("Other.Lib"));

        // Act
        bool result = CreateSut(commands).BuildAndUploadPackage();

        // Assert
        Assert.False(result);
        Assert.Empty(commands.Pushes);
        Assert.Contains($"No packages of project {_projectName} were created", ConsoleText(),
            StringComparison.Ordinal);
    }

    private PackageBuilder CreateSut(IPackageBuildCommands commands)
    {
        return new PackageBuilder(new Mock<ILogger>().Object, _parametersManager.Object, _projectName, commands);
    }

    private static void WriteProjectFile(string folder, string projectName)
    {
        string projectFolder = Path.Combine(folder, projectName);
        Directory.CreateDirectory(projectFolder);
        File.WriteAllText(Path.Combine(projectFolder, $"{projectName}.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk" />""");
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    //a package that the fake pack writes. A dependency without a version is a project reference, which gets the version
    //of the pack
    private sealed record FakePackage(string Id, params (string Id, string? Version)[] Dependencies);

    //instead of dotnet pack it writes the packages into the output folder, instead of dotnet nuget push it notes the
    //push and the packages in the folder
    private sealed class FakePackageBuildCommands : IPackageBuildCommands
    {
        private readonly FakePackage[] _packages;

        // ReSharper disable once ConvertToPrimaryConstructor
        public FakePackageBuildCommands(params FakePackage[] packages)
        {
            _packages = packages;
        }

        public string? PackageVersion { get; private set; }
        public List<string> Pushes { get; } = [];
        public List<string> FilesAtPush { get; } = [];

        public Result Pack(string solutionFileName, string outputFolderPath, string packageVersion)
        {
            PackageVersion = packageVersion;
            foreach (FakePackage package in _packages)
            {
                WritePackage(Path.Combine(outputFolderPath, $"{package.Id}.{packageVersion}.nupkg"), package,
                    packageVersion);
            }

            return Result.Success();
        }

        public Result NugetPush(string nupkgPath, string source, string? apiKey)
        {
            Pushes.Add($"{nupkgPath} {source} {apiKey}");
            FilesAtPush.AddRange(Directory
                .GetFiles(Path.GetDirectoryName(nupkgPath) ?? string.Empty, Path.GetFileName(nupkgPath))
                .Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal));
            return Result.Success();
        }

        //a nupkg with its nuspec at the root, as dotnet pack writes it
        private static void WritePackage(string packageFile, FakePackage package, string packageVersion)
        {
            string dependencies = string.Concat(package.Dependencies.Select(x =>
                $"""<dependency id="{x.Id}" version="{x.Version ?? packageVersion}" exclude="Build,Analyzers" />"""));
            string nuspec = $"""
                <?xml version="1.0" encoding="utf-8"?>
                <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                  <metadata>
                    <id>{package.Id}</id>
                    <version>{packageVersion}</version>
                    <dependencies>
                      <group targetFramework="net10.0">{dependencies}</group>
                    </dependencies>
                  </metadata>
                </package>
                """;

            // ReSharper disable once using
            using ZipArchive packageArchive = ZipFile.Open(packageFile, ZipArchiveMode.Create);
            // ReSharper disable once using
            using var writer = new StreamWriter(packageArchive.CreateEntry($"{package.Id}.nuspec").Open(),
                Encoding.UTF8);
            writer.Write(nuspec);
        }
    }
}
