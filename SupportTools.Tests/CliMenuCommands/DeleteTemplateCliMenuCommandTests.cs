using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class DeleteTemplateCliMenuCommandTests : IDisposable
{
    private const string TemplateName = "ApiTemplate";

    private readonly List<string> _confirmQuestions = [];
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();

    public DeleteTemplateCliMenuCommandTests()
    {
        _parameters = new SupportToolsParameters
        {
            AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
            {
                Templates = { [TemplateName] = new TemplateModel(), ["OtherTemplate"] = new TemplateModel() }
            }
        };
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

    //the command sits in the menu of the template and goes back to the templates list after the deletion
    [Fact]
    public void Constructor_WhenCalled_NamesTheCommandInTheTemplateMenu()
    {
        // Act
        DeleteTemplateCliMenuCommand sut = CreateSut(true);

        // Assert
        Assert.Equal("Delete Template", sut.Name);
        Assert.Equal(EMenuAction.LevelUp, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
    }

    //the whole parameters object is saved: saving only the project creator parameters would replace the whole
    //file (and IParametersManager.Parameters) with that part
    [Fact]
    public async Task RunBody_WhenConfirmed_RemovesTheTemplateAndSavesTheWholeParameters()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut(true));

        // Assert
        Assert.True(result);
        Assert.Equal(["OtherTemplate"], _parameters.AppProjectCreatorAllParameters!.Templates.Keys);
        Assert.Equal([$"This will Delete Template {TemplateName}. are you sure?"], _confirmQuestions);
        _parametersManager.Verify(
            x => x.Save(_parameters, $"Template {TemplateName} Deleted", null, It.IsAny<CancellationToken>()),
            Times.Once);
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunBody_WhenNotConfirmed_KeepsTheTemplateAndSavesNothing()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut(false));

        // Assert
        Assert.False(result);
        Assert.Equal(2, _parameters.AppProjectCreatorAllParameters!.Templates.Count);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task RunBody_WhenTemplateDoesNotExist_ReturnsFalseWithoutAsking()
    {
        // Arrange
        _parameters.AppProjectCreatorAllParameters!.Templates.Remove(TemplateName);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut(true));

        // Assert
        Assert.False(result);
        Assert.Empty(_confirmQuestions);
        VerifyNothingSaved();
        Assert.Contains($"[ERROR] Template {TemplateName} not found", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenProjectCreatorParametersAreMissing_ReturnsFalseWithoutAsking()
    {
        // Arrange
        _parameters.AppProjectCreatorAllParameters = null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut(true));

        // Assert
        Assert.False(result);
        Assert.Empty(_confirmQuestions);
        VerifyNothingSaved();
        Assert.Contains("[ERROR] Support Tools Parameters not found", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //the public constructor asks on the console, Enter meaning "no". The test host has no console input, so reading
    //the answer fails right after the question is printed
    [Fact]
    public async Task RunBody_WhenCreatedForTheConsole_AsksWithNoAsTheDefaultAnswer()
    {
        // Arrange
        var sut = new DeleteTemplateCliMenuCommand(_parametersManager.Object, TemplateName);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => CliMenuTestAccess.InvokeRunBody(sut));

        // Assert
        Assert.Contains($"This will Delete Template {TemplateName}. are you sure? (y/n)[n]: ",
            _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(2, _parameters.AppProjectCreatorAllParameters!.Templates.Count);
        VerifyNothingSaved();
    }

    private DeleteTemplateCliMenuCommand CreateSut(bool confirm)
    {
        return new DeleteTemplateCliMenuCommand(_parametersManager.Object, TemplateName, question =>
        {
            _confirmQuestions.Add(question);
            return confirm;
        });
    }

    private void VerifyNothingSaved()
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
}
