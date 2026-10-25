using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class ServerInstallationListCruderTests
{
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public ServerInstallationListCruderTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private ServerInstallationListCruder CreateSut(List<string> names)
    {
        return new ServerInstallationListCruder(_parametersManager.Object, names, "IPv4 Address", "IPv4 Addresses");
    }

    [Fact]
    public void Constructor_WithNames_NamesTheRecordsByThem()
    {
        // Act
        ServerInstallationListCruder sut = CreateSut([]);

        // Assert
        Assert.Equal("IPv4 Address", sut.CrudName);
        Assert.Equal("IPv4 Addresses", sut.CrudNamePlural);
    }

    [Fact]
    public void GetKeys_WhenCalled_ReturnsTheValuesOfTheGivenList()
    {
        // Arrange
        ServerInstallationListCruder sut = CreateSut(["192.168.10.6/24", "192.168.10.5/24"]);

        // Act
        List<string> result = sut.GetKeys();

        // Assert
        Assert.Equal(["192.168.10.5/24", "192.168.10.6/24"], result);
    }

    //the list is a list of the installation record, so a new value must reach that very list
    [Fact]
    public async Task AddRecordWithKey_WhenCalled_AddsTheValueToTheGivenList()
    {
        // Arrange
        List<string> addresses = ["192.168.10.5/24"];
        ServerInstallationListCruder sut = CreateSut(addresses);

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "192.168.10.6/24", new TextItemData());

        // Assert
        Assert.Equal(["192.168.10.5/24", "192.168.10.6/24"], addresses);
    }

    [Fact]
    public async Task Save_WhenCalled_SavesTheRootParameters()
    {
        // Arrange
        ServerInstallationListCruder sut = CreateSut([]);

        // Act
        bool result = await sut.Save("IPv4 Address added");

        // Assert
        Assert.True(result);
        _parametersManager.Verify(x => x.Save(_parameters, "IPv4 Address added", It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
