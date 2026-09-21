using System;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTools.CliMenuCommands;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommandTests : IDisposable
{
    private const string AppliedPatternName = "Applied";

    private readonly EditorConfigTestEnvironment _env = new();

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Apply this pattern to all projects that do not have an .editorconfig pattern specified",
            sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WhenProjectHasNoPattern_AppliesCommandPatternToItAndReturnsTrue()
    {
        // Arrange
        _env.AddProject("NoPattern", null, null);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(AppliedPatternName, _env.Parameters.Projects["NoPattern"].EditorConfigPatternName);
    }

    private ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand CreateSut()
    {
        return new ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand(new Mock<ILogger>().Object,
            AppliedPatternName, _env.ParametersManager.Object);
    }
}
