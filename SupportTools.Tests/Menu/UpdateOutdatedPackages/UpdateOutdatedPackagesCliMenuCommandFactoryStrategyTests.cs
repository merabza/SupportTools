using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.UpdateOutdatedPackages;
using Xunit;

namespace SupportTools.Tests.Menu.UpdateOutdatedPackages;

public sealed class UpdateOutdatedPackagesCliMenuCommandFactoryStrategyTests
{
    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesTheUpdateOutdatedPackagesCommand()
    {
        // Arrange
        var sut = new UpdateOutdatedPackagesCliMenuCommandFactoryStrategy(
            new Mock<ILogger<UpdateOutdatedPackagesCliMenuCommandFactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, new Mock<IParametersManager>().Object);

        // Act
        CliMenuCommand result = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<UpdateOutdatedPackagesCliMenuCommand>(result);
        Assert.Equal(UpdateOutdatedPackagesCliMenuCommand.MenuCommandName, result.Name);
    }
}
