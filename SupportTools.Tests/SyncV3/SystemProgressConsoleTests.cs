using System;
using System.Globalization;
using System.IO;
using LibGitWork.SyncV3;
using Xunit;

namespace SupportTools.Tests.SyncV3;

//the output of the test host is not a console: the text goes to Console.Out, while the color and the cursor cannot be set
//there. Width and CursorTop need a real console, so they are checked only by hand (CLAUDE.md, Git sync V3)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SystemProgressConsoleTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SystemProgressConsole _sut = new();

    public SystemProgressConsoleTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Write_WhenThereIsNoColor_WritesTheText()
    {
        // Act
        _sut.Write("RepoA  fetch", null);

        // Assert
        Assert.Equal("RepoA  fetch", _consoleOutput.ToString());
    }

    [Fact]
    public void Write_WhenAColorIsGiven_WritesTheTextAndKeepsTheColorOfTheConsole()
    {
        // Arrange
        ConsoleColor color = Console.ForegroundColor;

        // Act
        _sut.Write("RepoA  push failed", ConsoleColor.Red);

        // Assert
        Assert.Equal("RepoA  push failed", _consoleOutput.ToString());
        Assert.Equal(color, Console.ForegroundColor);
    }

    //the output of the test host is redirected (a pipe), and a pipe has no cursor: the call reaches the console API,
    //which refuses it. The action draws the board only when the output is not redirected
    [Fact]
    public void SetCursorPosition_WhenTheOutputIsNotAConsole_IsRefusedByTheConsole()
    {
        // Act
        Exception? exception = Record.Exception(() => _sut.SetCursorPosition(0, 0));

        // Assert
        Assert.IsType<IOException>(exception, false);
    }

    //hiding the cursor is only for the looks: a console that cannot do it does not stop the sync
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetCursorVisible_WhenTheConsoleCannotChangeIt_DoesNotThrow(bool visible)
    {
        // Act
        Exception? exception = Record.Exception(() => _sut.SetCursorVisible(visible));

        // Assert
        Assert.Null(exception);
    }
}
