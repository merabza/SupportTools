using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.RemoveUnusedPackageVersions;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.RemoveUnusedPackageVersions;

//every project is a solution folder in {temp}; its Directory.Packages.props and the consumers are written by the tests
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RemoveUnusedPackageVersionsCliMenuCommandTests : IDisposable
{
    private const string AppPropsContent = """
        <Project>
          <ItemGroup>
            <PackageVersion Include="Used" Version="1.0.0" />
            <PackageVersion Include="Unused" Version="1.0.0" />
            <PackageVersion Include="UpdatedOnly" Version="1.0.0" />
            <PackageVersion Include="Global" Version="1.0.0" />
            <PackageVersion Include="$(OwnPackage)" Version="1.0.0" />
            <PackageVersion Update="Updated" Version="2.0.0" />
          </ItemGroup>
          <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
            <PackageVersion Include="Conditional" Version="1.0.0" />
          </ItemGroup>
          <ItemGroup>
            <PackageVersion Include="AlsoUnused" Version="1.0.0" />
          </ItemGroup>
          <ItemGroup>
            <!-- kept for later -->
            <PackageVersion Include="Commented" Version="1.0.0" />
          </ItemGroup>
        </Project>
        """;

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public RemoveUnusedPackageVersionsCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_AsksBeforeRunningAndReloadsTheMenu()
    {
        // Act
        RemoveUnusedPackageVersionsCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Remove Unused Package Versions", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task GetActionDescription_WhenCalled_DescribesTheChange()
    {
        // Act
        string? result = await CliMenuTestAccess.InvokeGetActionDescription(CreateSut());

        // Assert
        Assert.Equal(
            "This process will analyze Directory.Packages.props files of all registered projects and remove PackageVersion entries that are not used by any project",
            result);
    }

    //a package used by Include, Update or a GlobalPackageReference stays; so do the conditional, the MSBuild property
    //and the Update-style entries; an ItemGroup left empty goes, one with a comment stays
    [Fact]
    public async Task RunBody_WhenPackagesAreUnused_RemovesOnlyTheSafeEntries()
    {
        // Arrange
        string props = CreateSolution("App", AppPropsContent, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Used" />
                <PackageReference Update="UpdatedOnly" />
              </ItemGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(props)!, "Directory.Build.targets"), """
            <Project>
              <ItemGroup>
                <GlobalPackageReference Include="Global" />
              </ItemGroup>
            </Project>
            """);
        //only the csproj, props and targets files are read as consumers
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(props)!, "Readme.txt"), "not xml");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        XDocument propsXml = XDocument.Load(props);
        Assert.Equal(["Used", "UpdatedOnly", "Global", "$(OwnPackage)", "Conditional"],
            propsXml.Descendants("PackageVersion").Select(x => (string?)x.Attribute("Include")).OfType<string>());
        Assert.Single(propsXml.Descendants("PackageVersion"), x => (string?)x.Attribute("Update") == "Updated");
        Assert.Equal(3, propsXml.Descendants("ItemGroup").Count());
        Assert.Single(propsXml.DescendantNodes().OfType<XComment>());
        string output = _consoleOutput.ToString();
        Assert.Contains("  Removed unused package version Unused", output, StringComparison.Ordinal);
        Assert.Contains("  Removed unused package version AlsoUnused", output, StringComparison.Ordinal);
        Assert.Contains("  Removed unused package version Commented", output, StringComparison.Ordinal);
        Assert.Contains(
            $"Package Conditional looks unused in {props}, but the PackageVersion element is conditional, left in place",
            output, StringComparison.Ordinal);
        Assert.Contains(
            "Remove unused package versions finished. Props files processed: 1, files changed: 1, entries removed: 3",
            output, StringComparison.Ordinal);
        VerifyLogged($"Removed 3 unused package versions from {props}");
        VerifyLogged(
            "Remove unused package versions finished. Props files processed: 1, files changed: 1, entries removed: 3");
    }

    //the whitespace before a removed element goes with it, so no empty line is left
    [Fact]
    public async Task RunBody_WhenAnEntryIsRemoved_LeavesNoEmptyLine()
    {
        // Arrange
        string props = CreateSolution("App", """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Used" Version="1.0.0" />
                <PackageVersion Include="Unused" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """, """<Project><ItemGroup><PackageReference Include="Used" /></ItemGroup></Project>""");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        string content = await File.ReadAllTextAsync(props);
        Assert.DoesNotContain("Unused", content, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\n[ \t]*\r?\n", content);
    }

    [Fact]
    public async Task RunBody_WhenEveryPackageIsUsed_ChangesNothing()
    {
        // Arrange
        const string propsContent =
            """<Project><ItemGroup><PackageVersion Include="Used" Version="1.0.0" /></ItemGroup></Project>""";
        string props = CreateSolution("App", propsContent,
            """<Project><ItemGroup><PackageReference Include="Used" /></ItemGroup></Project>""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(propsContent, await File.ReadAllTextAsync(props));
        Assert.Contains(
            "Remove unused package versions finished. Props files processed: 1, files changed: 0, entries removed: 0",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the consumers in bin and obj do not count, and a props file there is not processed
    [Fact]
    public async Task RunBody_WhenFilesAreInBinOrObj_IgnoresThem()
    {
        // Arrange
        string props = CreateSolution("App",
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""",
            """<Project><ItemGroup><PackageReference Include="Other" /></ItemGroup></Project>""");
        string solutionFolder = Path.GetDirectoryName(props)!;
        WriteFile(Path.Combine(solutionFolder, "Lib", "obj", "Lib.csproj"),
            """<Project><ItemGroup><PackageReference Include="Unused" /></ItemGroup></Project>""");
        string binProps = Path.Combine(solutionFolder, "Lib", "bin", "Directory.Packages.props");
        WriteFile(binProps,
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.DoesNotContain("Unused", await File.ReadAllTextAsync(props), StringComparison.Ordinal);
        Assert.Contains("Unused", await File.ReadAllTextAsync(binProps), StringComparison.Ordinal);
        Assert.Contains("Props files processed: 1, files changed: 1, entries removed: 1", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //the projects go in name order
    [Fact]
    public async Task RunBody_WhenThereAreSeveralProjects_ProcessesThemInNameOrder()
    {
        // Arrange
        string betaProps = CreateSolution("Beta",
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""",
            "<Project />");
        string alphaProps = CreateSolution("Alpha",
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""",
            "<Project />");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        string output = _consoleOutput.ToString();
        Assert.True(output.IndexOf($"Processing {alphaProps}", StringComparison.Ordinal) <
                    output.IndexOf($"Processing {betaProps}", StringComparison.Ordinal));
    }

    //only the whitespace before a removed element goes with it; other text stays
    [Fact]
    public async Task RunBody_WhenTextPrecedesARemovedEntry_KeepsTheText()
    {
        // Arrange
        string props = CreateSolution("App",
            """<Project><ItemGroup>note<PackageVersion Include="Unused" Version="1.0.0" /><PackageVersion Include="Used" Version="1.0.0" /></ItemGroup></Project>""",
            """<Project><ItemGroup><PackageReference Include="Used" /></ItemGroup></Project>""");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        string content = await File.ReadAllTextAsync(props);
        Assert.DoesNotContain("Unused", content, StringComparison.Ordinal);
        Assert.Contains("<ItemGroup>note<PackageVersion Include=\"Used\"", content, StringComparison.Ordinal);
    }

    //a props file that cannot be written is reported and left as it is
    [Fact]
    public async Task RunBody_WhenThePropsFileCannotBeWritten_ReportsItAndFails()
    {
        // Arrange
        const string propsContent =
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""";
        string props = CreateSolution("App", propsContent, "<Project />");
        File.SetAttributes(props, FileAttributes.ReadOnly);

        try
        {
            // Act
            bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

            // Assert
            Assert.False(result);
            Assert.Equal(propsContent, await File.ReadAllTextAsync(props));
            Assert.Contains("UnauthorizedAccessException thrown with message", _consoleOutput.ToString(),
                StringComparison.Ordinal);
        }
        finally
        {
            File.SetAttributes(props, FileAttributes.Normal);
        }
    }

    //a props file shared by two projects is processed once
    [Fact]
    public async Task RunBody_WhenTwoProjectsShareThePropsFile_ProcessesItOnce()
    {
        // Arrange
        string props = CreateSolution("App",
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""",
            "<Project />");
        string solutionFolder = Path.GetDirectoryName(props)!;
        _parameters.Projects["App2"] = new ProjectModel
        {
            ProjectFolderName = solutionFolder, SolutionFileName = Path.Combine(solutionFolder, "App2.slnx")
        };

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Equal(1, CountOccurrences(output, $"Processing {props}"));
        Assert.Contains("Props files processed: 1, files changed: 1, entries removed: 1", output,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenProjectIsRemote_SkipsIt()
    {
        // Arrange
        string props = CreateSolution("App",
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""",
            "<Project />");
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"),
            SolutionFileName = Path.Combine(Path.GetDirectoryName(props)!, "App.slnx")
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("Unused", await File.ReadAllTextAsync(props), StringComparison.Ordinal);
        Assert.Contains("1 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task RunBody_WhenSolutionFileNameIsNotSpecified_SkipsTheProject(string? solutionFileName)
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _tempFolder, SolutionFileName = solutionFileName
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("SolutionFileName does not specified for project App, skipped", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("App.slnx")]
    [InlineData(@"Missing\App.slnx")]
    public async Task RunBody_WhenSolutionFolderDoesNotExist_SkipsTheProject(string solutionFileName)
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _tempFolder,
            SolutionFileName = solutionFileName.Contains('\\', StringComparison.Ordinal)
                ? Path.Combine(_tempFolder, solutionFileName)
                : solutionFileName
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("Solution folder does not exists for project App, skipped", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenThereIsNoPropsFile_SkipsTheProject()
    {
        // Arrange
        string solutionFolder = Path.Combine(_tempFolder, "App");
        Directory.CreateDirectory(solutionFolder);
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = solutionFolder, SolutionFileName = Path.Combine(solutionFolder, "App.slnx")
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("Directory.Packages.props does not found for project App, skipped",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenThereIsNoCsprojUnderThePropsFile_LeavesItUnchangedAndFails()
    {
        // Arrange
        const string propsContent =
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""";
        string props = CreateSolution("App", propsContent, null);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(propsContent, await File.ReadAllTextAsync(props));
        Assert.Contains($"No csproj files found under {Path.GetDirectoryName(props)}, file left unchanged",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Include")]
    [InlineData("Update")]
    public async Task RunBody_WhenAReferenceUsesAnMsBuildProperty_LeavesThePropsFileUnchangedAndFails(
        string attributeName)
    {
        // Arrange
        const string propsContent =
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""";
        string props = CreateSolution("App", propsContent,
            $"""<Project><ItemGroup><PackageReference {attributeName}="$(MyPackage)" /></ItemGroup></Project>""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(propsContent, await File.ReadAllTextAsync(props));
        Assert.Contains(
            $"PackageReference with MSBuild property $(MyPackage) found in {Path.Combine(Path.GetDirectoryName(props)!, "App", "App.csproj")}, analysis is not reliable, file left unchanged",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenAConsumerIsNotValidXml_LeavesThePropsFileUnchangedAndFails()
    {
        // Arrange
        const string propsContent =
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""";
        string props = CreateSolution("App", propsContent, "<Project>");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(propsContent, await File.ReadAllTextAsync(props));
        Assert.Contains("XmlException thrown with message", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenThePropsFileIsNotValidXml_ReportsItAndFails()
    {
        // Arrange
        string props = CreateSolution("App", "<Project><ItemGroup>", "<Project />");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal("<Project><ItemGroup>", await File.ReadAllTextAsync(props));
    }

    [Fact]
    public async Task RunBody_WhenCancelled_Throws()
    {
        // Arrange
        CreateSolution("App",
            """<Project><ItemGroup><PackageVersion Include="Unused" Version="1.0.0" /></ItemGroup></Project>""",
            "<Project />");
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        Exception exception = await Record.ExceptionAsync(() =>
            CliMenuTestAccess.InvokeRunBody(CreateSut(), cancellationTokenSource.Token));

        // Assert
        Assert.IsType<OperationCanceledException>(exception.InnerException ?? exception);
        Assert.DoesNotContain("Processing", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private RemoveUnusedPackageVersionsCliMenuCommand CreateSut()
    {
        return new RemoveUnusedPackageVersionsCliMenuCommand(_logger.Object, _parametersManager.Object);
    }

    //{temp}\{name}\Directory.Packages.props and, when given, {temp}\{name}\{name}\{name}.csproj; returns the props file
    private string CreateSolution(string name, string propsContent, string? csprojContent)
    {
        string solutionFolder = Path.Combine(_tempFolder, name);
        string props = Path.Combine(solutionFolder, "Directory.Packages.props");
        WriteFile(props, propsContent);
        if (csprojContent is not null)
        {
            WriteFile(Path.Combine(solutionFolder, name, $"{name}.csproj"), csprojContent);
        }

        _parameters.Projects[name] = new ProjectModel
        {
            ProjectFolderName = solutionFolder, SolutionFileName = Path.Combine(solutionFolder, $"{name}.slnx")
        };
        return props;
    }

    private static void WriteFile(string fileName, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
        File.WriteAllText(fileName, content);
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }

    private void VerifyLogged(string message)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) => value.ToString() == message), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }
}
