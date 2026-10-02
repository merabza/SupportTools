using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

//checking and updating change the stored versions of the tools (updating even when it fails half way), so the
//result is always saved. No tool is registered here: the commands only read the installed tools list
[Collection(ConsoleCaptureCollection.Name)]
public sealed class UpdateToolsToLatestVersionCliMenuCommandsTests : IDisposable
{
    private const string SaveMessage = "Dotnet Tools versions saved";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<string> _questions = [];

    public UpdateToolsToLatestVersionCliMenuCommandsTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    //Check used to save only when it reported a change; when one tool fails it reports none, though the other
    //tools may have changed
    [Fact]
    public async Task CheckAll_WhenCalled_ChecksAndSavesTheWholeParameters()
    {
        // Arrange
        var sut = new CheckDotnetToolsVersionsCliMenuCommand(_parametersManager.Object);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.True(result);
        Assert.Equal("Check Dotnet Tools Versions...", sut.Name);
        Assert.Contains("Checking versions for all tools...", _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task UpdateAll_WhenConfirmed_UpdatesAndSavesTheWholeParameters()
    {
        // Arrange
        var sut = new UpdateAllToolsToLatestVersionCliMenuCommand(_parametersManager.Object, Answer(true));

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.True(result);
        Assert.Equal("Update All Tools To Latest Version", sut.Name);
        Assert.Equal(["Are you sure, you want to Update All Tools To Latest Version?"], _questions);
        Assert.Contains("All tools already are up to date.", _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task UpdateAll_WhenNotConfirmed_SavesNothing()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(
            new UpdateAllToolsToLatestVersionCliMenuCommand(_parametersManager.Object, Answer(false)));

        // Assert
        Assert.False(result);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task UpdateOne_WhenUpdateFails_ReturnsFalseAndStillSaves()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(
            new UpdateOneToolToLatestVersionCliMenuCommand(_parametersManager.Object, "MissingTool", Answer(true)));

        // Assert
        Assert.False(result);
        Assert.Contains("Tool with key MissingTool not found.", _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task UpdateOne_WhenNotConfirmed_SavesNothing()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(
            new UpdateOneToolToLatestVersionCliMenuCommand(_parametersManager.Object, "MissingTool", Answer(false)));

        // Assert
        Assert.False(result);
        VerifySaved(Times.Never());
    }

    //the command of one tool was named and asked as if it updated all the tools
    [Fact]
    public async Task UpdateOne_WhenRun_NamesTheToolInItsQuestion()
    {
        // Arrange
        var sut = new UpdateOneToolToLatestVersionCliMenuCommand(_parametersManager.Object, "MissingTool",
            Answer(false));

        // Act
        await CliMenuTestAccess.InvokeRunBody(sut);

        // Assert
        Assert.Equal("Update Tool To Latest Version", sut.Name);
        Assert.Equal(["Are you sure, you want to Update MissingTool To Latest Version?"], _questions);
    }

    //the public constructors ask on the console, Enter meaning "yes". The test host has no console input, so
    //reading the answer fails right after the question is printed
    [Fact]
    public async Task UpdateAll_WhenCreatedForTheConsole_AsksWithYesAsTheDefaultAnswer()
    {
        // Arrange
        var sut = new UpdateAllToolsToLatestVersionCliMenuCommand(_parametersManager.Object);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => CliMenuTestAccess.InvokeRunBody(sut));

        // Assert
        Assert.Contains("Are you sure, you want to Update All Tools To Latest Version? (y/n)[y]: ",
            _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task UpdateOne_WhenCreatedForTheConsole_AsksWithYesAsTheDefaultAnswer()
    {
        // Arrange
        var sut = new UpdateOneToolToLatestVersionCliMenuCommand(_parametersManager.Object, "MissingTool");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => CliMenuTestAccess.InvokeRunBody(sut));

        // Assert
        Assert.Contains("Are you sure, you want to Update MissingTool To Latest Version? (y/n)[y]: ",
            _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifySaved(Times.Never());
    }

    //records every question and gives the same answer to all of them
    private Func<string, bool> Answer(bool answer)
    {
        return question =>
        {
            _questions.Add(question);
            return answer;
        };
    }

    private void VerifySaved(Times times)
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), times);
        _parametersManager.Verify(x => x.Save(_parameters, SaveMessage, null, It.IsAny<CancellationToken>()), times);
    }
}
