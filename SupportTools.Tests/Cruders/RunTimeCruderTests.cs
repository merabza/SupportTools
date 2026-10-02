using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class RunTimeCruderTests
{
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public RunTimeCruderTests()
    {
        _parameters.RunTimes["win-x64"] = "Windows 64 bit";
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheRunTimeRecords()
    {
        // Act
        var sut = new RunTimeCruder(_parametersManager.Object, []);

        // Assert
        Assert.Equal("RunTime", sut.CrudName);
        Assert.Equal("RunTimes", sut.CrudNamePlural);
    }

    [Fact]
    public void Constructor_WhenCalled_EditsTheGivenDictionary()
    {
        // Arrange
        Dictionary<string, string> runTimes = new() { ["osx-arm64"] = "macOS on Apple silicon" };

        // Act
        var sut = new RunTimeCruder(_parametersManager.Object, runTimes);

        // Assert
        Assert.True(sut.ContainsRecordWithKey("osx-arm64"));
        Assert.False(sut.ContainsRecordWithKey("win-x64"));
        Assert.Equal("macOS on Apple silicon", sut.GetStatusFor("osx-arm64"));
    }

    [Fact]
    public async Task Create_WhenCalled_EditsTheRunTimesOfTheParameters()
    {
        // Arrange
        var sut = RunTimeCruder.Create(_parametersManager.Object);

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "linux-x64", new TextItemData { Text = "Linux 64 bit" });

        // Assert
        Assert.True(sut.ContainsRecordWithKey("win-x64"));
        Assert.Equal("Linux 64 bit", _parameters.RunTimes["linux-x64"]);
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsTheRunTimesAndOffersToGenerateTheStandardOnes()
    {
        // Act
        CliMenuSet listMenu = RunTimeCruder.Create(_parametersManager.Object).GetListMenu();

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand)];
        Assert.Equal(["win-x64"], commands.OfType<ItemSubMenuCliMenuCommand>().Select(x => x.Name));
        Assert.Single(commands.OfType<GenerateStandardRunTimesCliMenuCommand>());
    }
}
