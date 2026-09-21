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
public sealed class UpdateEditorConfigFilesCliMenuCommandTests : IDisposable
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
        UpdateEditorConfigFilesCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Update .editorconfig Files", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WhenEditorConfigFileIsWrong_OverwritesItWithTemplateAndReturnsTrue()
    {
        // Arrange
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent,
            await File.ReadAllTextAsync(_env.EditorConfigFileName));
    }

    //the tool action reports to the console only when it is told to use it
    [Fact]
    public async Task RunBody_WhenRun_ReportsProgressOnConsole()
    {
        // Arrange
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("Update .editorconfig Files Started...", _env.ConsoleText(), StringComparison.Ordinal);
    }

    private UpdateEditorConfigFilesCliMenuCommand CreateSut()
    {
        return new UpdateEditorConfigFilesCliMenuCommand(new Mock<ILogger>().Object, _env.ParametersManager.Object);
    }
}
