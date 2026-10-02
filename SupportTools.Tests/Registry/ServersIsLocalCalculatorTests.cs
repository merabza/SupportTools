using LibSupportToolsServerWork.Registry;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Registry;

public sealed class ServersIsLocalCalculatorTests
{
    //today's file: two computers are marked as local, because the file was copied between them
    private readonly SupportToolsParameters _parameters = new()
    {
        Servers =
        {
            ["PAZISI"] = new ServerDataModel { IsLocal = true },
            ["Merinson"] = new ServerDataModel { IsLocal = true },
            ["dl360"] = new ServerDataModel { IsLocal = false }
        }
    };

    [Fact]
    public void Recalculate_WhenCurrentMachineServerNameIsSet_MarksOnlyThatServerAsLocal()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "PAZISI";

        // Act
        ServersIsLocalCalculator.Recalculate(_parameters);

        // Assert
        Assert.True(_parameters.Servers["PAZISI"].IsLocal);
        Assert.False(_parameters.Servers["Merinson"].IsLocal);
        Assert.False(_parameters.Servers["dl360"].IsLocal);
    }

    [Fact]
    public void Recalculate_WhenCurrentMachineServerNameDiffersByCase_StillMatchesServer()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "merinson";

        // Act
        ServersIsLocalCalculator.Recalculate(_parameters);

        // Assert
        Assert.True(_parameters.Servers["Merinson"].IsLocal);
        Assert.False(_parameters.Servers["PAZISI"].IsLocal);
    }

    [Fact]
    public void Recalculate_WhenCurrentMachineServerNameIsNotInServers_MarksNoServerAsLocal()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "NewComputer";

        // Act
        ServersIsLocalCalculator.Recalculate(_parameters);

        // Assert
        Assert.All(_parameters.Servers.Values, x => Assert.False(x.IsLocal));
    }

    //an old parameters file has no CurrentMachineServerName: the stored values keep working
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Recalculate_WhenCurrentMachineServerNameIsEmpty_KeepsStoredValues(string? currentMachineServerName)
    {
        // Arrange
        _parameters.CurrentMachineServerName = currentMachineServerName;

        // Act
        ServersIsLocalCalculator.Recalculate(_parameters);

        // Assert
        Assert.True(_parameters.Servers["PAZISI"].IsLocal);
        Assert.True(_parameters.Servers["Merinson"].IsLocal);
        Assert.False(_parameters.Servers["dl360"].IsLocal);
    }
}
