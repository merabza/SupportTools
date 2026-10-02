using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class ServerDataCruderTests
{
    private const string IsLocalFieldName = "Is Local";

    private readonly SupportToolsParameters _parameters = new()
    {
        Servers =
        {
            ["PAZISI"] = new ServerDataModel { IsLocal = true },
            ["Merinson"] = new ServerDataModel { IsLocal = true }
        }
    };

    private readonly Mock<IParametersManager> _parametersManager = new();

    public ServerDataCruderTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public void Constructor_WhenCreated_NamesTheServerRecords()
    {
        // Act
        ServerDataCruder sut = CreateSut();

        // Assert
        Assert.Equal("Server", sut.CrudName);
        Assert.Equal("Servers", sut.CrudNamePlural);
    }

    //the order is the order of the fields in the server record menu
    [Fact]
    public void Constructor_WhenCreated_RegistersFieldEditorsInMenuOrder()
    {
        // Arrange
        string[] expected =
        [
            nameof(ServerDataModel.IsLocal), nameof(ServerDataModel.WebAgentName),
            nameof(ServerDataModel.WebAgentInstallerName), nameof(ServerDataModel.FilesUserName),
            nameof(ServerDataModel.FilesUsersGroupName), nameof(ServerDataModel.Runtime),
            nameof(ServerDataModel.ServerSideDownloadFolder), nameof(ServerDataModel.ServerSideDeployFolder)
        ];

        // Act
        ServerDataCruder sut = CreateSut();

        // Assert
        Assert.Equal(expected, CliMenuTestAccess.GetFieldEditors(sut).Select(x => x.PropertyName));
    }

    [Fact]
    public void Create_WhenCalled_EditsServersOfParameters()
    {
        // Arrange
        string[] expected = ["Merinson", "PAZISI"];

        // Act
        var sut = ServerDataCruder.Create(new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object);

        // Assert
        Assert.Equal(expected, sut.GetKeys());
    }

    //an old parameters file has no CurrentMachineServerName: IsLocal is edited by hand as before
    [Fact]
    public void Constructor_WhenCurrentMachineServerNameIsEmpty_EditsIsLocal()
    {
        // Act
        ServerDataCruder sut = CreateSut();

        // Assert
        Assert.True(GetIsLocalEditor(sut).Enabled);
    }

    //a new record asks its fields before CheckFieldsEnables runs, so the constructor must already hide IsLocal
    [Fact]
    public void Constructor_WhenCurrentMachineServerNameIsSet_HidesIsLocal()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "PAZISI";

        // Act
        ServerDataCruder sut = CreateSut();

        // Assert
        Assert.False(GetIsLocalEditor(sut).Enabled);
    }

    [Fact]
    public void GetItemMenu_WhenCurrentMachineServerNameIsEmpty_OffersIsLocal()
    {
        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu("Merinson");

        // Assert
        Assert.Contains(IsLocalFieldName, CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.MenuItemName));
    }

    [Fact]
    public void GetItemMenu_WhenCurrentMachineServerNameIsSet_DoesNotOfferIsLocal()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "PAZISI";

        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu("Merinson");

        // Assert
        Assert.DoesNotContain(IsLocalFieldName, CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.MenuItemName));
    }

    [Fact]
    public void GetItemMenu_WhenCurrentMachineServerNameIsSetAfterCreation_DoesNotOfferIsLocal()
    {
        // Arrange
        ServerDataCruder sut = CreateSut();
        _parameters.CurrentMachineServerName = "PAZISI";

        // Act
        CliMenuSet itemMenu = sut.GetItemMenu("Merinson");

        // Assert
        Assert.DoesNotContain(IsLocalFieldName, CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.MenuItemName));
    }

    //without CurrentMachineServerName a new server is offered as local
    [Fact]
    public void IsLocalEditor_WhenNewServerGetsDefaults_MarksItAsLocal()
    {
        // Arrange
        var newServer = new ServerDataModel();

        // Act
        GetIsLocalEditor(CreateSut()).SetDefault(newServer);

        // Assert
        Assert.True(newServer.IsLocal);
    }

    //IsLocal cannot be set by hand then, so a new (or renamed) server gets it from CurrentMachineServerName
    [Fact]
    public async Task AddRecordWithKey_WhenServerIsThisComputer_MarksOnlyItAsLocal()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "NewComputer";
        var newServer = new ServerDataModel { IsLocal = false };

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(CreateSut(), "NewComputer", newServer);

        // Assert
        Assert.True(newServer.IsLocal);
        Assert.False(_parameters.Servers["PAZISI"].IsLocal);
        Assert.False(_parameters.Servers["Merinson"].IsLocal);
    }

    [Fact]
    public async Task AddRecordWithKey_WhenCurrentMachineServerNameIsEmpty_KeepsEnteredIsLocal()
    {
        // Arrange
        var newServer = new ServerDataModel { IsLocal = true };

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(CreateSut(), "NewComputer", newServer);

        // Assert
        Assert.True(newServer.IsLocal);
        Assert.True(_parameters.Servers["PAZISI"].IsLocal);
    }

    private ServerDataCruder CreateSut()
    {
        return new ServerDataCruder(new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object, _parameters.Servers);
    }

    private static FieldEditor GetIsLocalEditor(ServerDataCruder cruder)
    {
        return CliMenuTestAccess.GetFieldEditors(cruder).Single(x => x.PropertyName == nameof(ServerDataModel.IsLocal));
    }
}
