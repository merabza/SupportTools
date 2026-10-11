using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliMenu.CliMenuCommands;
using Microsoft.Extensions.DependencyInjection;
using SupportTools.AutoSync;
using SupportTools.Tests.AutoSync;
using SupportTools.Tests.Registry.Sync;
using Xunit;

namespace SupportTools.Tests;

//the main menu writes the deferred start-up summary of auto-sync (D2) under the header, once
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SupportToolsMenuBuilderTests
{
    [Fact]
    public async Task BuildMainMenu_WhenStartSummaryWaits_WritesItOnlyAtTheFirstBuild()
    {
        // Arrange
        using var context = new AutoSyncTestContext();
        context.EnvironmentsAdapter.Server["Dev"] = new FakeContract("Dev", "d", 1);
        RegistryAutoSync autoSync = context.CreateAutoSync();
        await autoSync.PullOnStart(true);
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = new ServiceCollection().AddSingleton(autoSync)
            .BuildServiceProvider();
        var sut = new SupportToolsMenuBuilder(serviceProvider);

        // Act
        CliMenuSet? firstMenu = await sut.BuildMainMenu();
        CliMenuSet? secondMenu = await sut.BuildMainMenu();

        // Assert
        Assert.NotNull(firstMenu);
        Assert.NotNull(secondMenu);
        Assert.Equal("Registry auto-sync: 1 record taken from SupportToolsServer",
            Assert.Single(context.ConsoleLines()));
    }

    //without auto-sync and without command strategies the main menu holds only its Exit item
    [Fact]
    public async Task BuildMainMenu_WhenCalled_CreatesTheMainMenuWithExit()
    {
        // Arrange
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = new ServiceCollection().BuildServiceProvider();
        var sut = new SupportToolsMenuBuilder(serviceProvider);

        // Act
        CliMenuSet? result = await sut.BuildMainMenu();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Main Menu", result.Caption);
        Assert.IsType<ExitCliMenuCommand>(Assert.Single(CliMenuTestAccess.GetMenuItems(result)).CliMenuCommand);
    }
}
