using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SupportTools.Menu;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu;

//D3: the group commands work on the local projects by default, with an explicit choice for all of them
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RemoteOnlyProjectsTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), $"RemoteOnlyProjectsTests_{Guid.NewGuid():N}");

    public RemoteOnlyProjectsTests()
    {
        Directory.CreateDirectory(_tempFolder);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void AskInclude_WhenEveryProjectIsLocal_DoesNotAsk()
    {
        // Arrange
        List<string> questions = [];

        // Act
        bool result = RemoteOnlyProjects.AskInclude([Local("A"), Local("B")], (question, _) =>
        {
            questions.Add(question);
            return true;
        });

        // Assert
        Assert.False(result);
        Assert.Empty(questions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AskInclude_WhenRemoteProjectsExist_AsksWithDefaultNoAndReturnsTheAnswer(bool answer)
    {
        // Arrange
        string? question = null;
        bool? defaultValue = null;

        // Act
        bool result = RemoteOnlyProjects.AskInclude([Local("A"), Remote("B"), Remote("C")], (text, value) =>
        {
            question = text;
            defaultValue = value;
            return answer;
        });

        // Assert
        Assert.Equal(answer, result);
        Assert.Equal("Include 2 remote-only projects (their gits will be cloned to this computer)?", question);
        Assert.False(defaultValue);
    }

    [Fact]
    public void Skip_WhenRemoteProjectsExist_ReturnsLocalProjectsAndReportsTheSkippedCount()
    {
        // Act
        List<KeyValuePair<string, ProjectModel>> result =
            RemoteOnlyProjects.Skip([Remote("A"), Local("B"), Remote("C"), Local("D")], null);

        // Assert
        Assert.Equal(["B", "D"], result.Select(x => x.Key));
        Assert.Contains("2 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Skip_WhenEveryProjectIsLocal_ReportsNothing()
    {
        // Act
        List<KeyValuePair<string, ProjectModel>> result = RemoteOnlyProjects.Skip([Local("A")], null);

        // Assert
        Assert.Equal(["A"], result.Select(x => x.Key));
        Assert.DoesNotContain("skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private KeyValuePair<string, ProjectModel> Local(string name)
    {
        return new KeyValuePair<string, ProjectModel>(name, new ProjectModel { ProjectFolderName = _tempFolder });
    }

    private KeyValuePair<string, ProjectModel> Remote(string name)
    {
        return new KeyValuePair<string, ProjectModel>(name,
            new ProjectModel { ProjectFolderName = Path.Combine(_tempFolder, name) });
    }
}
