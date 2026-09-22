using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SupportToolsData;
using Xunit;

namespace SupportTools.Tests;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ArgumentsAnalyzerTests : IDisposable
{
    private const string ProjectName = "MyProject";
    private const string ServerName = "dl360";

    private readonly StringWriter _consoleError = new(CultureInfo.InvariantCulture);
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleError;
    private readonly TextWriter _originalConsoleOutput;
    private readonly ArgumentsAnalyzer _sut = new();

    public ArgumentsAnalyzerTests()
    {
        _originalConsoleOutput = Console.Out;
        _originalConsoleError = Console.Error;
        Console.SetOut(_consoleOutput);
        Console.SetError(_consoleError);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        Console.SetError(_originalConsoleError);
        _consoleOutput.Dispose();
        _consoleError.Dispose();
    }

    [Fact]
    public async Task Analysis_WhenThereAreNoArguments_ContinuesWithoutAnyValue()
    {
        // Act
        bool result = await _sut.Analysis([]);

        // Assert
        Assert.True(result);
        Assert.Null(_sut.ParametersFileName);
        Assert.Null(_sut.ProjectName);
        Assert.Null(_sut.ServerName);
        Assert.Null(_sut.ProjectTool);
        Assert.Null(_sut.ServerTool);
        Assert.Equal(0, _sut.ExitCode);
    }

    [Theory]
    [InlineData("--use")]
    [InlineData("-u")]
    public async Task Analysis_WhenParametersFileIsSpecified_KeepsItsName(string optionName)
    {
        // Act
        bool result = await _sut.Analysis([optionName, "par.json"]);

        // Assert
        Assert.True(result);
        Assert.Equal("par.json", _sut.ParametersFileName);
    }

    [Fact]
    public async Task Analysis_WhenParametersFileNameIsWhiteSpace_TreatsItAsNotSpecified()
    {
        // Act
        bool result = await _sut.Analysis(["--use", "   "]);

        // Assert
        Assert.True(result);
        Assert.Null(_sut.ParametersFileName);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    public async Task Analysis_WhenHelpIsRequested_ShowsUsageAndStops(string optionName)
    {
        // Act
        bool result = await _sut.Analysis([optionName]);

        // Assert
        Assert.False(result);
        Assert.Equal(0, _sut.ExitCode);
        Assert.Contains("Usage:", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("--project", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenOptionIsUnknown_StopsWithParseErrorCode()
    {
        // Act
        bool result = await _sut.Analysis(["--nosuchoption"]);

        // Assert
        Assert.False(result);
        Assert.Equal(1, _sut.ExitCode);
        Assert.Contains("Unrecognized command or argument", ConsoleErrorText(), StringComparison.Ordinal);
        Assert.Contains("Usage:", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenProjectIsSpecifiedWithoutRun_WarnsAndContinues()
    {
        // Act
        bool result = await _sut.Analysis(["--project", ProjectName]);

        // Assert
        Assert.True(result);
        Assert.Equal(ProjectName, _sut.ProjectName);
        Assert.Contains("--project (-p) works only together with --run (-r), it will be ignored", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenServerIsSpecifiedWithoutRun_WarnsAndContinues()
    {
        // Act
        bool result = await _sut.Analysis(["--server", ServerName]);

        // Assert
        Assert.True(result);
        Assert.Equal(ServerName, _sut.ServerName);
        Assert.Contains("--server (-s) works only together with --run (-r), it will be ignored", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("RecreateDevDatabase")]
    [InlineData("recreatedevdatabase")]
    public async Task Analysis_WhenProjectToolIsRequested_KeepsItIgnoringCase(string toolName)
    {
        // Act
        bool result = await _sut.Analysis(["-p", ProjectName, "-r", toolName]);

        // Assert
        Assert.True(result);
        Assert.Equal(EProjectTools.RecreateDevDatabase, _sut.ProjectTool);
        Assert.Null(_sut.ServerTool);
        Assert.Equal(0, _sut.ExitCode);
        Assert.DoesNotContain("--server (-s)", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenServerToolIsRequestedWithServer_KeepsBoth()
    {
        // Act
        bool result = await _sut.Analysis(["-p", ProjectName, "-s", ServerName, "-r", "ServiceStarter"]);

        // Assert
        Assert.True(result);
        Assert.Equal(EProjectServerTools.ServiceStarter, _sut.ServerTool);
        Assert.Null(_sut.ProjectTool);
        Assert.Equal(ServerName, _sut.ServerName);
        Assert.DoesNotContain("is not used for tool", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenServerIsSpecifiedForProjectTool_WarnsButContinues()
    {
        // Act
        bool result = await _sut.Analysis(["-p", ProjectName, "-s", ServerName, "-r", "RecreateDevDatabase"]);

        // Assert
        Assert.True(result);
        Assert.Equal(EProjectTools.RecreateDevDatabase, _sut.ProjectTool);
        Assert.Contains($"--server (-s) is not used for tool {EProjectTools.RecreateDevDatabase}", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("NoSuchTool")]
    [InlineData("2")]
    public async Task Analysis_WhenToolNameIsUnknown_StopsAndShowsHelp(string toolName)
    {
        // Act
        bool result = await _sut.Analysis(["-p", ProjectName, "-r", toolName]);

        // Assert
        Assert.False(result);
        Assert.Equal(5, _sut.ExitCode);
        Assert.Contains($"Tool with name {toolName} does not exists", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("Usage:", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenRunHasNoProject_StopsAndShowsHelp()
    {
        // Act
        bool result = await _sut.Analysis(["-r", "RecreateDevDatabase"]);

        // Assert
        Assert.False(result);
        Assert.Equal(5, _sut.ExitCode);
        Assert.Contains("--project (-p) must be specified together with --run (-r)", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Contains("Usage:", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenServerToolHasNoServer_StopsAndShowsHelp()
    {
        // Act
        bool result = await _sut.Analysis(["-p", ProjectName, "-r", "ServiceStarter"]);

        // Assert
        Assert.False(result);
        Assert.Equal(5, _sut.ExitCode);
        Assert.Contains($"--server (-s) must be specified for server tool {EProjectServerTools.ServiceStarter}",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("Usage:", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Analysis_WhenRunValueIsWhiteSpace_IsTreatedAsNoRun()
    {
        // Act
        bool result = await _sut.Analysis(["-p", ProjectName, "-r", "  "]);

        // Assert
        Assert.True(result);
        Assert.Null(_sut.ProjectTool);
        Assert.Null(_sut.ServerTool);
    }

    [Fact]
    public void ShowHelp_WhenArgumentsAreNotAnalyzedYet_WritesNothing()
    {
        // Act
        _sut.ShowHelp();

        // Assert
        Assert.Equal(string.Empty, ConsoleText());
    }

    [Fact]
    public async Task ShowHelp_AfterAnalysis_WritesUsageWithAllOptions()
    {
        // Arrange
        await _sut.Analysis([]);

        // Act
        _sut.ShowHelp();

        // Assert
        Assert.Contains("--use", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("--project", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("--server", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains(nameof(EProjectTools.RecreateDevDatabase), ConsoleText(), StringComparison.Ordinal);
        Assert.Contains(nameof(EProjectServerTools.ServiceStarter), ConsoleText(), StringComparison.Ordinal);
    }
    [Fact]
    public async Task ShowHelp_AfterAnalysis_WritesDescriptionOfEveryOption()
    {
        // Arrange
        await _sut.Analysis([]);

        // Act
        _sut.ShowHelp();

        // Assert
        Assert.Contains("SupportTools - support tools for .NET projects.", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("File name for use as parameters json.", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("Project name, for which the tool must be run.", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("Server name or ServerName|EnvironmentName.", ConsoleText(), StringComparison.Ordinal);
        Assert.Contains("Run this project or server tool for the project", ConsoleText(), StringComparison.Ordinal);
    }

    private string ConsoleText()

    {
        return _consoleOutput.ToString();
    }

    private string ConsoleErrorText()
    {
        return _consoleError.ToString();
    }
}
