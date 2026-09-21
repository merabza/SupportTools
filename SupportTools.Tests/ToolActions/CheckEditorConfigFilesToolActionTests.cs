using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTools.ToolActions;
using Xunit;

namespace SupportTools.Tests.ToolActions;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class CheckEditorConfigFilesToolActionTests : IDisposable
{
    private readonly EditorConfigTestEnvironment _env = new();

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public async Task Run_WhenWrongFilesFound_ListsThemWithoutChangingThem()
    {
        // Arrange
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Contains("wrong .editorconfig files are found:", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Contains(_env.EditorConfigFileName, _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Equal("changed", await File.ReadAllTextAsync(_env.EditorConfigFileName));
    }

    [Fact]
    public async Task Run_WhenNothingIsWrong_ReportsIt()
    {
        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Contains("--wrong .editorconfig files are not found", _env.ConsoleText(), StringComparison.Ordinal);
    }

    private CheckEditorConfigFilesToolAction CreateSut()
    {
        return new CheckEditorConfigFilesToolAction(new Mock<ILogger>().Object, _env.ParametersManager.Object, true);
    }
}
