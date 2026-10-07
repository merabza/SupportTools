using System.Collections.Generic;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.SupportToolsServerEdit;
using SupportTools.Menu.SyncRegistry;
using Xunit;

namespace SupportTools.Tests.Menu.SyncRegistry;

public sealed class SyncRegistryCliMenuCommandFactoryStrategyTests
{
    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesTheSyncCommand()
    {
        // Arrange
        var sut = new SyncRegistryCliMenuCommandFactoryStrategy(
            new Mock<ILogger<SyncRegistryCliMenuCommandFactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, new Mock<IParametersManager>().Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<SyncRegistryCliMenuCommand>(command);
        Assert.Equal(SyncRegistryCliMenuCommand.CommandName, command.Name);
    }

    //strategies are looked up by type name, so the main menu shows the command only if its name is listed
    [Fact]
    public void MainMenuCommandFactoryStrategyNames_ListStrategyRightAfterTheServerEditor()
    {
        // Act
        List<string> strategyNames = MenuData.MainMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(SyncRegistryCliMenuCommandFactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(SupportToolsServerEditorListCliMenuCommandFactoryStrategy), strategyNames[index - 1]);
    }
}
