using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.Services.RecentCommands;
using AppCliTools.CliTools.Services.RecentCommands.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ParametersManagement.LibParameters;
using SupportTools.AutoSync;
using SupportTools.DependencyInjection;
using SupportTools.Menu.CheckAllProjectsBuild;
using SupportTools.Menu.DistributeAllPackages;
using SupportTools.Menu.RemoveUnusedPackageVersions;
using SupportTools.Menu.ReversePackageDistribution;
using SupportTools.Menu.SyncAllProjectsAllGits;
using SupportTools.Menu.UpdateOutdatedPackages;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.DependencyInjection;

public sealed class SupportToolsServicesTests
{
    //D2: the menu and --run (ProjectToolRunner) take IParametersManager from DI, so the decorator is the only one, and
    //auto-sync saves the sync state through the inner manager
    [Fact]
    public async Task AddServices_WhenBuilt_MakesTheAutoSyncDecoratorTheOnlyParametersManager()
    {
        // Arrange
        var parameters = new SupportToolsParameters();
        var services = new ServiceCollection();
        services.AddServices("SupportToolsTests", parameters, @"D:\Fake\SupportTools.json");

        // Act
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var result = serviceProvider.GetRequiredService<IParametersManager>();

        // Assert
        var decorator =
            Assert.IsType<AutoSyncParametersManager>(Assert.Single(serviceProvider.GetServices<IParametersManager>()));
        Assert.Same(decorator, result);
        Assert.Same(parameters, result.Parameters);
        Assert.Equal(@"D:\Fake\SupportTools.json", result.ParametersFileName);
        Assert.Same(serviceProvider.GetRequiredService<SupportToolsMainParametersManager>(),
            AutoSyncParametersManager.WithoutAutoSync(result));
        Assert.Same(serviceProvider.GetRequiredService<RegistryAutoSync>(),
            serviceProvider.GetRequiredService<RegistryAutoSync>());
    }

    [Fact]
    public async Task AddServices_WhenBuilt_NamesTheApplication()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServices("SupportToolsTests", new SupportToolsParameters(), @"D:\Fake\SupportTools.json");

        // Act
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var result = serviceProvider.GetRequiredService<IApplication>();

        // Assert
        Assert.Equal("SupportToolsTests", result.AppName);
    }

    //the strategies take the decorator from DI, so none of them may cast it to the concrete ParametersManager
    [Fact]
    public async Task AddServices_WhenBuilt_BuildsTheWholeMainMenuWithTheAutoSyncDecorator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddServices("SupportToolsTests", new SupportToolsParameters(), @"D:\Fake\SupportTools.json");
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var sut = new SupportToolsMenuBuilder(serviceProvider);

        // Act
        CliMenuSet? result = await sut.BuildMainMenu();

        // Assert
        Assert.NotNull(result);
        List<string> commandTypeNames =
            [.. CliMenuTestAccess.GetMenuItems(result).Select(x => x.CliMenuCommand.GetType().Name)];
        Assert.Contains(nameof(SyncAllProjectsAllGitsCliMenuCommandV2), commandTypeNames);
        Assert.Contains(nameof(UpdateOutdatedPackagesCliMenuCommand), commandTypeNames);
        Assert.Contains(nameof(DistributeAllPackagesCliMenuCommand), commandTypeNames);
        Assert.Contains(nameof(ReversePackageDistributionCliMenuCommand), commandTypeNames);
        Assert.Contains(nameof(RemoveUnusedPackageVersionsCliMenuCommand), commandTypeNames);
        Assert.Contains(nameof(CheckAllProjectsBuildCliMenuCommand), commandTypeNames);
    }

    //the recent commands of the menu take their file and count from the parameters
    [Fact]
    public async Task AddServices_WhenBuilt_RegistersTheRecentCommandsOfTheParameters()
    {
        // Arrange
        var parameters = new SupportToolsParameters
        {
            RecentCommandsFileName = @"D:\Fake\RecentCommands.json", RecentCommandsCount = 15
        };
        var services = new ServiceCollection();
        services.AddServices("SupportToolsTests", parameters, @"D:\Fake\SupportTools.json");

        // Act
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        RecentCommandOptions result = serviceProvider.GetRequiredService<IOptions<RecentCommandOptions>>().Value;

        // Assert
        Assert.Equal(@"D:\Fake\RecentCommands.json", result.RecentCommandsFileName);
        Assert.Equal(15, result.RecentCommandsCount);
        Assert.NotNull(serviceProvider.GetService<IRecentCommandsService>());
    }
}
