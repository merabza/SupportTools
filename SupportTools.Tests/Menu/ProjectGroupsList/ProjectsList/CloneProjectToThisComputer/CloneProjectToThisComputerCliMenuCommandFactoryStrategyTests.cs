using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class CloneProjectToThisComputerCliMenuCommandFactoryStrategyTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

    public CloneProjectToThisComputerCliMenuCommandFactoryStrategyTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    //the command works on the project that is current in the menu
    [Fact]
    public async Task CreateMenuCommand_WhenCalled_CreatesTheCloneCommandForTheCurrentProject()
    {
        // Arrange
        var parametersManager = new Mock<IParametersManager>();
        parametersManager.SetupGet(x => x.Parameters).Returns(new SupportToolsParameters());
        var sut = new CloneProjectToThisComputerCliMenuCommandFactoryStrategy(
            new Mock<ILogger<CloneProjectToThisComputerCliMenuCommandFactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, new SupportToolsMenuParameters { ProjectName = "App" },
            parametersManager.Object);

        // Act
        CliMenuCommand result = sut.CreateMenuCommand();
        await result.Run();

        // Assert
        Assert.IsType<CloneProjectToThisComputerCliMenuCommand>(result);
        Assert.Equal("Clone Project To This Computer", result.Name);
        Assert.Equal(EMenuAction.Reload, result.MenuActionOnBodySuccess);
        Assert.Contains("Project App does not exist", _consoleOutput.ToString(), StringComparison.Ordinal);
    }
}
