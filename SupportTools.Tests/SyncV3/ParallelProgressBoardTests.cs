using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGitWork.SyncV3;
using Xunit;

namespace SupportTools.Tests.SyncV3;

//the board on a simulated screen 30 columns wide: every board line is cut to 29 characters
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ParallelProgressBoardTests
{
    private readonly FakeProgressConsole _console = new(30, 10);

    [Fact]
    public void StartLine_WhenTheTextChanges_UpdatesTheSameRow()
    {
        // Arrange
        using var sut = new ParallelProgressBoard(_console);
        sut.SetHeader("Syncing 2 gits: 0 done");
        ProgressBoardLine repoA = sut.StartLine("RepoA  start");
        sut.StartLine("RepoB  start");

        // Act
        repoA.SetText("RepoA  fetch");

        // Assert
        Assert.Equal(["Syncing 2 gits: 0 done", "RepoA  fetch", "RepoB  start"], _console.Lines);
        Assert.Equal(3, _console.CursorTop);
    }

    [Fact]
    public void WriteLine_WhenTheBoardIsShown_WritesTheLineAboveIt()
    {
        // Arrange
        _console.WriteLine("Started");
        using var sut = new ParallelProgressBoard(_console);
        sut.SetHeader("Syncing 2 gits: 0 done");
        sut.StartLine("RepoA  fetch");

        // Act
        sut.WriteLine("RepoB  D:\\B: pulled");
        sut.WriteLine("RepoC  D:\\C: pushed");

        // Assert
        Assert.Equal(
            ["Started", "RepoB  D:\\B: pulled", "RepoC  D:\\C: pushed", "Syncing 2 gits: 0 done", "RepoA  fetch"],
            _console.Lines);
    }

    //a finished job leaves no row behind: the rows below the smaller board are cleared
    [Fact]
    public void Dispose_OfALine_RemovesItsRow()
    {
        // Arrange
        using var sut = new ParallelProgressBoard(_console);
        sut.SetHeader("Syncing 2 gits: 0 done");
        ProgressBoardLine repoA = sut.StartLine("RepoA  fetch");
        sut.StartLine("RepoB  push");

        // Act
        repoA.Dispose();

        // Assert
        Assert.Equal(["Syncing 2 gits: 0 done", "RepoB  push"], _console.Lines);
        Assert.Equal(2, _console.CursorTop);
    }

    //after the board the console goes on right below the permanent lines, and the cursor is shown again
    [Fact]
    public void Dispose_WhenCalled_ClearsTheBoardAndKeepsThePermanentLines()
    {
        // Arrange
        var sut = new ParallelProgressBoard(_console);
        sut.SetHeader("Syncing 1 gits: 0 done");
        sut.StartLine("RepoA  fetch");
        sut.WriteLine("RepoA  D:\\A: pulled");
        Assert.False(_console.CursorVisible);

        // Act
        sut.Dispose();
        _console.WriteLine("Summary");

        // Assert
        Assert.Equal(["RepoA  D:\\A: pulled", "Summary"], _console.Lines);
        Assert.True(_console.CursorVisible);
    }

    //a board line never wraps, so the board keeps its place on the screen
    [Fact]
    public void StartLine_WhenTheTextIsLongerThanTheWindow_CutsIt()
    {
        // Arrange
        using var sut = new ParallelProgressBoard(_console);

        // Act
        sut.StartLine("RepoA  fetch  D:\\1WorkDotnet\\SupportTools\\SupportTools");

        // Assert
        Assert.Equal(["RepoA  fetch  D:\\1WorkDotn..."], _console.Lines);
    }

    //the permanent lines scroll the screen; the board stays at the bottom without copies of its lines
    [Fact]
    public void WriteLine_WhenTheScreenScrolls_KeepsOneBoardAtTheBottom()
    {
        // Arrange
        using var sut = new ParallelProgressBoard(_console);
        sut.SetHeader("Checking 20 folders");
        ProgressBoardLine line = sut.StartLine("RepoA  check");

        // Act
        for (int i = 0; i < 20; i++)
        {
            sut.WriteLine(string.Create(CultureInfo.InvariantCulture, $"result {i}"));
            line.SetText(string.Create(CultureInfo.InvariantCulture, $"RepoA  step {i}"));
        }

        // Assert
        Assert.True(_console.ScrolledRowCount > 0);
        Assert.Equal(
        [
            "result 13", "result 14", "result 15", "result 16", "result 17", "result 18", "result 19",
            "Checking 20 folders", "RepoA  step 19"
        ], _console.Lines);
    }

    //without a console (redirected output) only the permanent lines are written, to Console.Out
    [Fact]
    public void WriteLine_WhenThereIsNoConsole_WritesOnlyThePermanentLines()
    {
        // Arrange
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        TextWriter originalOutput = Console.Out;
        Console.SetOut(output);
        try
        {
            using var sut = new ParallelProgressBoard(null);
            sut.SetHeader("Syncing 1 gits: 0 done");
            using ProgressBoardLine line = sut.StartLine("RepoA  start");
            line.SetText("RepoA  fetch");

            // Act
            sut.WriteLine("RepoA  D:\\A: pulled");
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        // Assert
        Assert.Equal($"RepoA  D:\\A: pulled{Environment.NewLine}", output.ToString());
    }

    [Theory]
    [InlineData("abc", 5, "abc  ")]
    [InlineData("abcde", 5, "abcde")]
    [InlineData("abcdefgh", 5, "ab...")]
    [InlineData("abcdefgh", 4, "a...")]
    [InlineData("abcdefgh", 3, "abc")]
    [InlineData("abcdefgh", 2, "ab")]
    [InlineData("a\tb\r\nc", 7, "a b  c ")]
    public void Fit_WhenCalled_GivesExactlyTheWidth(string text, int width, string expected)
    {
        // Act
        string result = ParallelProgressBoard.Fit(text, width);

        // Assert
        Assert.Equal(expected, result);
    }

    //an unchanged header or text is not drawn again
    [Fact]
    public void SetHeaderAndSetText_WhenTheTextIsTheSame_DoNotRedraw()
    {
        // Arrange
        using var sut = new ParallelProgressBoard(_console);
        sut.SetHeader("Syncing 1 gits: 0 done");
        ProgressBoardLine line = sut.StartLine("RepoA  fetch");
        int writeCount = _console.Writes.Count;

        // Act
        sut.SetHeader("Syncing 1 gits: 0 done");
        line.SetText("RepoA  fetch");

        // Assert
        Assert.Equal(writeCount, _console.Writes.Count);
        Assert.Equal("RepoA  fetch", line.Text);
    }

    [Fact]
    public void Dispose_OfALineThatIsAlreadyRemoved_DoesNotRedraw()
    {
        // Arrange
        using var sut = new ParallelProgressBoard(_console);
        ProgressBoardLine line = sut.StartLine("RepoA  fetch");
        line.Dispose();
        int writeCount = _console.Writes.Count;

        // Act
        line.Dispose();

        // Assert
        Assert.Equal(writeCount, _console.Writes.Count);
        Assert.Empty(_console.Lines);
    }

    //the color reaches only the permanent line; the board lines and the line ends have none
    [Fact]
    public void WriteLine_WhenAColorIsGiven_WritesTheLineInIt()
    {
        // Arrange
        using var sut = new ParallelProgressBoard(_console);
        sut.StartLine("RepoA  push");

        // Act
        sut.WriteLine("RepoA  D:\\A: push failed", ConsoleColor.Red);

        // Assert
        Assert.Contains(("RepoA  D:\\A: push failed", ConsoleColor.Red), _console.Writes);
        Assert.All(_console.Writes.Where(x => x.Text != "RepoA  D:\\A: push failed"), x => Assert.Null(x.Color));
    }

    //without board lines the permanent line is written where the cursor is
    [Fact]
    public void WriteLine_WhenTheBoardIsEmpty_WritesOnlyTheLine()
    {
        // Arrange
        _console.WriteLine("Started");
        using var sut = new ParallelProgressBoard(_console);

        // Act
        sut.WriteLine("RepoA  D:\\A: cloned");

        // Assert
        Assert.Equal(["Started", "RepoA  D:\\A: cloned"], _console.Lines);
        Assert.Equal(2, _console.CursorTop);
    }

    //a window of one column still gets one character per board line
    [Fact]
    public void StartLine_WhenTheWindowIsOneColumnWide_WritesOneCharacter()
    {
        // Arrange
        var console = new FakeProgressConsole(1, 5);
        using var sut = new ParallelProgressBoard(console);

        // Act
        sut.StartLine("RepoA");

        // Assert
        Assert.Contains(("R" + Environment.NewLine, null), console.Writes);
    }

    //without a console nothing is drawn, and nothing breaks at the end
    [Fact]
    public void Dispose_WhenThereIsNoConsole_WritesNothing()
    {
        // Arrange
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        TextWriter originalOutput = Console.Out;
        Console.SetOut(output);
        try
        {
            var sut = new ParallelProgressBoard(null);
            sut.SetHeader("Syncing 1 gits: 0 done");
            sut.StartLine("RepoA  fetch");

            // Act
            sut.Dispose();
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        // Assert
        Assert.Empty(output.ToString());
    }
}
