using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportTools.FieldEditors;
using SupportTools.Menu;
using SupportTools.Menu.BaGetter;
using SupportTools.Menu.ServerInstallations;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ServerInstallations;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ServerInstallationCruderTests : IDisposable
{
    private const string InstallationKey = "oslo";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly ServerInstallationModel _installation = new() { Hostname = "oslo", DomainName = "cyberia.ge" };
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public ServerInstallationCruderTests()
    {
        _parameters.ServerInstallations[InstallationKey] = _installation;
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    private ServerInstallationCruder CreateSut()
    {
        return ServerInstallationCruder.Create(_parametersManager.Object);
    }

    private static Dictionary<string, bool> GetEnabledFields(ServerInstallationCruder sut)
    {
        return CliMenuTestAccess.GetFieldEditors(sut).ToDictionary(x => x.PropertyName, x => x.Enabled);
    }

    private static FieldEditor GetFieldEditor(ServerInstallationCruder sut, string propertyName)
    {
        return CliMenuTestAccess.GetFieldEditors(sut).Single(x => x.PropertyName == propertyName);
    }

    //the field editor keeps the hash function of its field in a private field
    private static Func<string, string> GetHashPassword(FieldEditor fieldEditor)
    {
        return (Func<string, string>)typeof(PasswordHashFieldEditor)
            .GetField("_hashPassword", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fieldEditor)!;
    }

    //New asks for the fields in this order, and the item menu lists them in the same order
    [Fact]
    public void Constructor_WhenCalled_EditsEveryFieldOfTheInstallationInOrder()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(
        [
            nameof(ServerInstallationModel.Hostname), nameof(ServerInstallationModel.DomainName),
            nameof(ServerInstallationModel.RealName), nameof(ServerInstallationModel.UserName),
            nameof(ServerInstallationModel.PasswordHash), nameof(ServerInstallationModel.SshAuthorizedKeys),
            nameof(ServerInstallationModel.SshAllowPassword), nameof(ServerInstallationModel.NetworkInterfaceName),
            nameof(ServerInstallationModel.IpV4Addresses), nameof(ServerInstallationModel.Gateway),
            nameof(ServerInstallationModel.NameServers), nameof(ServerInstallationModel.Timezone),
            nameof(ServerInstallationModel.Locale), nameof(ServerInstallationModel.KeyboardLayout),
            nameof(ServerInstallationModel.GrubTimeoutSeconds),
            nameof(ServerInstallationModel.AutoinstallUserDataFilePath),
            nameof(ServerInstallationModel.InstallDiskSerial), nameof(ServerInstallationModel.FilesFolder),
            nameof(ServerInstallationModel.InstallVirtualmin), nameof(ServerInstallationModel.InstallWebmin),
            nameof(ServerInstallationModel.DnsPrimaryNameServer), nameof(ServerInstallationModel.EmailToAddress),
            nameof(ServerInstallationModel.InstallMariaDb), nameof(ServerInstallationModel.MariaDbRootPasswordHash),
            nameof(ServerInstallationModel.InstallPostgreSql), nameof(ServerInstallationModel.InstallMsSqlServer),
            nameof(ServerInstallationModel.SqlServerEdition), nameof(ServerInstallationModel.SqlServerSaPasswordHash),
            nameof(ServerInstallationModel.SqlServerPort), nameof(ServerInstallationModel.OpenSqlServerPort),
            nameof(ServerInstallationModel.InstallSqlServerClient), nameof(ServerInstallationModel.InstallDotnetSdk),
            nameof(ServerInstallationModel.InstallDotnetRuntime)
        ], fieldEditors.Select(x => x.PropertyName));
    }

    //the two numbers are asked when an installation is created, so their defaults are seen and can be accepted
    [Fact]
    public void Constructor_WhenCalled_AsksTheNumbersOnCreate()
    {
        // Act
        ServerInstallationCruder sut = CreateSut();

        // Assert
        Assert.True(GetFieldEditor(sut, nameof(ServerInstallationModel.GrubTimeoutSeconds)).EnterFieldDataOnCreate);
        Assert.True(GetFieldEditor(sut, nameof(ServerInstallationModel.SqlServerPort)).EnterFieldDataOnCreate);
    }

    //each list is edited in a list menu of its own, over the list of the record
    [Theory]
    [InlineData(nameof(ServerInstallationModel.SshAuthorizedKeys), "SSH Authorized Keys", "New SSH Authorized Key")]
    [InlineData(nameof(ServerInstallationModel.IpV4Addresses), "IPv4 Addresses", "New IPv4 Address")]
    [InlineData(nameof(ServerInstallationModel.NameServers), "Name Servers", "New Name Server")]
    public void ListFieldEditors_WhenOpened_ShowTheValuesOfTheRecord(string propertyName, string caption,
        string newItemName)
    {
        // Arrange
        var installation = new ServerInstallationModel
        {
            SshAuthorizedKeys = { "first" }, IpV4Addresses = { "first" }, NameServers = { "first" }
        };

        // Act
        CliMenuSet listMenu = GetFieldEditor(CreateSut(), propertyName).GetSubMenu(installation)!;

        // Assert
        Assert.Equal(caption, listMenu.Caption);
        List<string> names = [.. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand.Name)];
        Assert.Equal(newItemName, names[0]);
        Cruder listCruder = CliMenuTestAccess.GetListMenuCruder(listMenu);
        Assert.Equal(["first"], listCruder.GetKeys());
    }

    //the passwords are stored only as hashes, each in the format its program reads
    [Theory]
    [InlineData(nameof(ServerInstallationModel.PasswordHash), "$6$")]
    [InlineData(nameof(ServerInstallationModel.MariaDbRootPasswordHash), "*")]
    [InlineData(nameof(ServerInstallationModel.SqlServerSaPasswordHash), "0x0200")]
    public void PasswordFieldEditors_WhenHashing_UseTheFormatOfTheirProgram(string propertyName, string prefix)
    {
        // Arrange
        FieldEditor fieldEditor = GetFieldEditor(CreateSut(), propertyName);

        // Act
        string result = GetHashPassword(fieldEditor)("secret");

        // Assert
        Assert.IsType<PasswordHashFieldEditor>(fieldEditor);
        Assert.StartsWith(prefix, result, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesTheServerInstallationsList()
    {
        // Arrange
        var sut = new ServerInstallationsCliMenuCommandFactoryStrategy(_parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<CruderListCliMenuCommand>(command);
        Assert.Equal("Server Installations", command.Name);
    }

    //strategies are looked up by type name, so the main menu shows the command only if its name is listed
    [Fact]
    public void MainMenuCommandFactoryStrategyNames_ListStrategyRightAfterBaGetter()
    {
        // Act
        List<string> strategyNames = MenuData.MainMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(ServerInstallationsCliMenuCommandFactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(BaGetterSubMenuCliMenuCommandFactoryStrategy), strategyNames[index - 1]);
    }

    [Fact]
    public void GetListMenu_WhenCalled_OffersANewInstallationAndListsTheInstallations()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        List<string> names = [.. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand.Name)];
        Assert.Equal("New Server Installation", names[0]);
        Assert.Equal(InstallationKey, names[1]);
    }

    [Fact]
    public void GetStatusFor_WithHostnameAndDomain_ReturnsTheFullHostName()
    {
        // Act
        string? result = CreateSut().GetStatusFor(InstallationKey);

        // Assert
        Assert.Equal("oslo.cyberia.ge", result);
    }

    [Fact]
    public void GetStatusFor_WithoutDomain_ReturnsTheHostName()
    {
        // Arrange
        _installation.DomainName = null;

        // Act
        string? result = CreateSut().GetStatusFor(InstallationKey);

        // Assert
        Assert.Equal("oslo", result);
    }

    //a full name of only the domain would be misleading
    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void GetStatusFor_WithoutHostName_ReturnsNoStatus(string? hostname)
    {
        // Arrange
        _installation.Hostname = hostname;

        // Act
        string? result = CreateSut().GetStatusFor(InstallationKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetStatusFor_WithUnknownName_ReturnsNoStatus()
    {
        // Act
        string? result = CreateSut().GetStatusFor("missing");

        // Assert
        Assert.Null(result);
        Assert.Contains("Server Installation with Name missing is not exists", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void GetItemMenu_WhenCalled_OffersToReadGenerateAndWriteToUsb()
    {
        // Act
        CliMenuSet itemMenu = CreateSut().GetItemMenu(InstallationKey);

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(itemMenu).Select(x => x.CliMenuCommand)];
        Assert.Single(commands.OfType<ReadFromAutoinstallUserDataCliMenuCommand>());
        Assert.Single(commands.OfType<GenerateServerInstallationFilesCliMenuCommand>());
        Assert.Single(commands.OfType<WriteServerInstallationToUsbCliMenuCommand>());
        Assert.Single(commands.OfType<RecordKeyEditorCliMenuCommand>());
    }

    [Fact]
    public void GetItemMenu_WithoutApps_ShowsOnlyTheFieldsThatAreUsed()
    {
        // Arrange
        ServerInstallationCruder sut = CreateSut();

        // Act
        sut.GetItemMenu(InstallationKey);

        // Assert
        Dictionary<string, bool> enabled = GetEnabledFields(sut);
        Assert.True(enabled[nameof(ServerInstallationModel.InstallWebmin)]);
        Assert.True(enabled[nameof(ServerInstallationModel.InstallSqlServerClient)]);
        Assert.True(enabled[nameof(ServerInstallationModel.InstallDotnetRuntime)]);
        Assert.False(enabled[nameof(ServerInstallationModel.DnsPrimaryNameServer)]);
        Assert.False(enabled[nameof(ServerInstallationModel.EmailToAddress)]);
        Assert.False(enabled[nameof(ServerInstallationModel.MariaDbRootPasswordHash)]);
        Assert.False(enabled[nameof(ServerInstallationModel.SqlServerEdition)]);
        Assert.False(enabled[nameof(ServerInstallationModel.SqlServerSaPasswordHash)]);
        Assert.False(enabled[nameof(ServerInstallationModel.SqlServerPort)]);
        Assert.False(enabled[nameof(ServerInstallationModel.OpenSqlServerPort)]);
    }

    //Virtualmin brings Webmin, and SQL Server always gets sqlcmd, which sets the sa hash
    [Fact]
    public void GetItemMenu_WithVirtualminMariaDbAndSqlServer_ShowsTheirFields()
    {
        // Arrange
        _installation.InstallVirtualmin = true;
        _installation.InstallMariaDb = true;
        _installation.InstallMsSqlServer = true;
        ServerInstallationCruder sut = CreateSut();

        // Act
        sut.GetItemMenu(InstallationKey);

        // Assert
        Dictionary<string, bool> enabled = GetEnabledFields(sut);
        Assert.False(enabled[nameof(ServerInstallationModel.InstallWebmin)]);
        Assert.False(enabled[nameof(ServerInstallationModel.InstallSqlServerClient)]);
        Assert.True(enabled[nameof(ServerInstallationModel.DnsPrimaryNameServer)]);
        Assert.True(enabled[nameof(ServerInstallationModel.EmailToAddress)]);
        Assert.True(enabled[nameof(ServerInstallationModel.MariaDbRootPasswordHash)]);
        Assert.True(enabled[nameof(ServerInstallationModel.SqlServerEdition)]);
        Assert.True(enabled[nameof(ServerInstallationModel.SqlServerSaPasswordHash)]);
        Assert.True(enabled[nameof(ServerInstallationModel.SqlServerPort)]);
        Assert.True(enabled[nameof(ServerInstallationModel.OpenSqlServerPort)]);
    }

    //the SDK contains the runtime
    [Fact]
    public void GetItemMenu_WithDotnetSdk_HidesTheRuntime()
    {
        // Arrange
        _installation.InstallDotnetSdk = true;
        ServerInstallationCruder sut = CreateSut();

        // Act
        sut.GetItemMenu(InstallationKey);

        // Assert
        Assert.False(GetEnabledFields(sut)[nameof(ServerInstallationModel.InstallDotnetRuntime)]);
    }

    //"New Server Installation" gives the new record the default of every field editor before the input: the lists
    //must stay lists (Validate went through them and failed on null) and the GRUB timeout must not become 0
    [Fact]
    public void SetDefault_OfEveryFieldEditor_GivesTheModelDefaultsAndValidateWorks()
    {
        // Arrange
        var installation = new ServerInstallationModel();

        // Act
        foreach (FieldEditor fieldEditor in CliMenuTestAccess.GetFieldEditors(CreateSut()))
        {
            fieldEditor.SetDefault(installation);
        }

        // Assert
        Assert.Empty(installation.SshAuthorizedKeys);
        Assert.Empty(installation.IpV4Addresses);
        Assert.Empty(installation.NameServers);
        Assert.Equal(ServerInstallationModel.DefaultGrubTimeoutSeconds, installation.GrubTimeoutSeconds);
        Assert.Contains("Hostname is required", ServerInstallationFilesGenerator.Validate(installation));
    }

    [Fact]
    public void Constructor_WhenCalled_StoresPasswordsOnlyAsHashes()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(
        [
            nameof(ServerInstallationModel.PasswordHash), nameof(ServerInstallationModel.MariaDbRootPasswordHash),
            nameof(ServerInstallationModel.SqlServerSaPasswordHash)
        ], fieldEditors.OfType<PasswordHashFieldEditor>().Select(x => x.PropertyName));
    }
}
