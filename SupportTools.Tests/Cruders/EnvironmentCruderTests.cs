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

public sealed class EnvironmentCruderTests
{
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public EnvironmentCruderTests()
    {
        _parameters.Environments["Development"] = "Local development";
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheEnvironmentRecords()
    {
        // Act
        var sut = new EnvironmentCruder(_parametersManager.Object, []);

        // Assert
        Assert.Equal("Environment", sut.CrudName);
        Assert.Equal("Environments", sut.CrudNamePlural);
    }

    [Fact]
    public void Constructor_WhenCalled_EditsTheGivenDictionary()
    {
        // Arrange
        Dictionary<string, string> environments = new() { ["Testing"] = "Test servers" };

        // Act
        var sut = new EnvironmentCruder(_parametersManager.Object, environments);

        // Assert
        Assert.True(sut.ContainsRecordWithKey("Testing"));
        Assert.False(sut.ContainsRecordWithKey("Development"));
        Assert.Equal("Test servers", sut.GetStatusFor("Testing"));
    }

    [Fact]
    public async Task Create_WhenCalled_EditsTheEnvironmentsOfTheParameters()
    {
        // Arrange
        EnvironmentCruder sut = EnvironmentCruder.Create(_parametersManager.Object);

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "Production", new TextItemData { Text = "Live servers" });

        // Assert
        Assert.True(sut.ContainsRecordWithKey("Development"));
        Assert.Equal("Live servers", _parameters.Environments["Production"]);
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsTheEnvironmentsAndOffersToGenerateTheStandardOnes()
    {
        // Act
        CliMenuSet listMenu = EnvironmentCruder.Create(_parametersManager.Object).GetListMenu();

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand)];
        Assert.Equal(["Development"], commands.OfType<ItemSubMenuCliMenuCommand>().Select(x => x.Name));
        Assert.Single(commands.OfType<GenerateStandardEnvironmentsCliMenuCommand>());
    }
}
