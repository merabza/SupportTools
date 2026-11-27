using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliParametersApiClientsEdit.FieldEditors;
using AppCliTools.CliTools.CliMenuCommands;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportTools.Models;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Cruders;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ServerDataCruderTests : IDisposable
{
    private const string IsLocalFieldName = "Is Local";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;

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

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
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
            nameof(ServerDataModel.ServerSideDownloadFolder), nameof(ServerDataModel.ServerSideDeployFolder),
            nameof(ServerDataModel.IsDecommissioned)
        ];

        // Act
        ServerDataCruder sut = CreateSut();

        // Assert
        Assert.Equal(expected, CliMenuTestAccess.GetFieldEditors(sut).Select(x => x.PropertyName));
    }

    //a server may have no web agent and no installer (a local computer), so both editors offer the (None) choice
    [Fact]
    public void Constructor_WhenCreated_OffersNoneForBothWebAgents()
    {
        // Arrange
        string[] expected = [nameof(ServerDataModel.WebAgentName), nameof(ServerDataModel.WebAgentInstallerName)];
        FieldInfo useNoneField =
            typeof(ApiClientNameFieldEditor).GetField("_useNone", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(expected,
            fieldEditors.OfType<ApiClientNameFieldEditor>().Where(x => (bool)useNoneField.GetValue(x)!)
                .Select(x => x.PropertyName));
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

    //a decommissioned server stays as a sample of its old configuration: the Servers list and the server choices mark it
    [Fact]
    public void GetStatusFor_WhenServerIsDecommissioned_MarksIt()
    {
        // Arrange
        _parameters.Servers["dl360"] = new ServerDataModel { IsDecommissioned = true };

        // Act
        string? status = CreateSut().GetStatusFor("dl360");

        // Assert
        Assert.Equal("Decommissioned", status);
    }

    [Fact]
    public void GetStatusFor_WhenServerIsInUse_ReturnsNoStatus()
    {
        // Act
        string? status = CreateSut().GetStatusFor("PAZISI");

        // Assert
        Assert.Null(status);
    }

    //the Servers list and the server choices ask only for listed names: an unknown name is no error either
    [Fact]
    public void GetStatusFor_WhenServerIsMissing_ReturnsNoStatusAndWritesNoError()
    {
        // Act
        string? status = CreateSut().GetStatusFor("Missing");

        // Assert
        Assert.Null(status);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    //the Servers list of the parameters editor also counts the places that use a server (ServerUsages)
    [Theory]
    [InlineData(false, 0, "Usage count: 0")]
    [InlineData(false, 2, "Usage count: 2")]
    [InlineData(true, 2, "Decommissioned, Usage count: 2")]
    public void GetStatusFor_WhenUsagesAreGiven_AddsTheUsageCount(bool isDecommissioned, int usageCount,
        string expected)
    {
        // Arrange
        _parameters.Servers["PAZISI"].IsDecommissioned = isDecommissioned;
        ServerDataCruder sut = CreateSut(name =>
            name == "PAZISI"
                ? [.. Enumerable.Range(1, usageCount).Select(x => new ServerUsage($"P{x}", $"P{x}"))]
                : [new ServerUsage("Wrong", "Wrong")]);

        // Act
        string? status = sut.GetStatusFor("PAZISI");

        // Assert
        Assert.Equal(expected, status);
    }

    //the places come right after the fields; choosing one goes to its menu
    [Fact]
    public void GetItemMenu_WhenUsagesAreGiven_ListsThePlacesAsMenuLinksAfterTheFields()
    {
        // Arrange
        const string hiddenPlace = "G1/Remote/PAZISI|Prod (Hide Remote Only Projects is on)";
        ServerDataCruder sut = CreateSut(name =>
            name == "PAZISI"
                ?
                [
                    new ServerUsage("G1/App/PAZISI|Prod", "G1/App/PAZISI|Prod"),
                    new ServerUsage(hiddenPlace, "Support Tools Parameters Editor")
                ]
                : []);

        // Act
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(sut.GetItemMenu("PAZISI"));

        // Assert
        Assert.Equal(["Is Decommissioned", "G1/App/PAZISI|Prod", hiddenPlace, "Exit to Servers menu"],
            menuItems.Select(x => x.MenuItemName).TakeLast(4));
        Assert.Equal(["G1/App/PAZISI|Prod", "Support Tools Parameters Editor"],
            menuItems.Select(x => x.CliMenuCommand).OfType<InfoCliMenuCommand>().Select(x => x.GetMenuLinkToGo()));
    }

    //the server choices (ServerDataNameFieldEditor) create the cruder without the places
    [Fact]
    public void GetItemMenu_WithoutUsages_ListsNoPlaces()
    {
        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu("PAZISI");

        // Assert
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(itemMenu), x => x.CliMenuCommand is InfoCliMenuCommand);
    }

    private ServerDataCruder CreateSut(Func<string, IReadOnlyList<ServerUsage>>? getUsages = null)
    {
        return new ServerDataCruder(new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object, _parameters.Servers, getUsages);
    }

    private static FieldEditor GetIsLocalEditor(ServerDataCruder cruder)
    {
        return CliMenuTestAccess.GetFieldEditors(cruder).Single(x => x.PropertyName == nameof(ServerDataModel.IsLocal));
    }
}
