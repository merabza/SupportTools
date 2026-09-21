using System;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTools.CliMenuCommands;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class CheckEditorConfigFilesCliMenuCommandTests : IDisposable
{
    private readonly EditorConfigTestEnvironment _env = new();

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        CheckEditorConfigFilesCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Check .editorconfig Files", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WhenWrongFilesExist_ListsThemAndReturnsTrue()
    {
        // Arrange
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains(_env.EditorConfigFileName, _env.ConsoleText(), StringComparison.Ordinal);
    }

    //the tool action reports to the console only when it is told to use it
    [Fact]
    public async Task RunBody_WhenRun_ReportsProgressOnConsole()
    {
        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("Check .editorconfig Files Started...", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public void CountStatus_WhenNothingIsWrong_ShowsZero()
    {
        // Arrange
        CheckEditorConfigFilesCliMenuCommand sut = CreateSut();

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("0", sut.StatusString);
    }

    [Fact]
    public void CountStatus_WhenFilesAreWrong_ShowsTheirCount()
    {
        // Arrange
        File.WriteAllText(_env.EditorConfigFileName, "changed");
        _env.AddProject("Second", EditorConfigTestEnvironment.PatternName, null);
        CheckEditorConfigFilesCliMenuCommand sut = CreateSut();

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("2", sut.StatusString);
    }

    //the count can change outside the menu (git, editors), so the menu must be redrawn after every status refresh
    [Fact]
    public void CountStatus_WhenCalled_SetsMenuActionToReload()
    {
        // Arrange
        CheckEditorConfigFilesCliMenuCommand sut = CreateSut();

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal(EMenuAction.Reload, sut.MenuAction);
    }

    private CheckEditorConfigFilesCliMenuCommand CreateSut()
    {
        return new CheckEditorConfigFilesCliMenuCommand(new Mock<ILogger>().Object, _env.ParametersManager.Object);
    }
}
