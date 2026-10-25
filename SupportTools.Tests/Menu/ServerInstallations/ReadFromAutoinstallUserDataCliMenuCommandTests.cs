using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ServerInstallations;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ServerInstallations;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ReadFromAutoinstallUserDataCliMenuCommandTests : IDisposable
{
    private const string InstallationName = "oslo";

    private const string OneDiskAndEthernet = """
                                              autoinstall:
                                                network:
                                                  ethernets:
                                                    enp3s0: {}
                                                storage:
                                                  config:
                                                  - serial: SERIAL1
                                                    path: /dev/nvme0n1
                                                    type: disk
                                              """;

    private const string TwoDisksAndEthernets = """
                                                autoinstall:
                                                  network:
                                                    ethernets:
                                                      enp3s0: {}
                                                      enp4s0: {}
                                                  storage:
                                                    config:
                                                    - serial: SERIAL1
                                                      path: /dev/nvme0n1
                                                      type: disk
                                                    - serial: SERIAL2
                                                      type: disk
                                                """;

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<(string FieldName, string? DefaultValue)> _filePathPrompts = [];
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"ReadUserDataTests-{Guid.NewGuid():N}");
    private readonly ServerInstallationModel _installation = new();
    private readonly List<(string FieldName, List<string> Names, string? DefaultValue)> _menus = [];
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private string? _filePathAnswer;
    private bool _saveResult = true;
    private Func<string, List<string>, string?> _select = (_, names) => names[0];

    public ReadFromAutoinstallUserDataCliMenuCommandTests()
    {
        Directory.CreateDirectory(_folder);
        _parameters.ServerInstallations[InstallationName] = _installation;
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(() => _saveResult);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_folder, true);
    }

    private string WriteUserData(string yaml)
    {
        string path = Path.Combine(_folder, "autoinstall-user-data");
        File.WriteAllText(path, yaml);
        return path;
    }

    private ReadFromAutoinstallUserDataCliMenuCommand CreateSut()
    {
        return new ReadFromAutoinstallUserDataCliMenuCommand(_parametersManager.Object, InstallationName,
            (fieldName, defaultValue) =>
            {
                _filePathPrompts.Add((fieldName, defaultValue));
                return _filePathAnswer;
            }, (fieldName, menuSet, defaultValue) =>
            {
                List<string> names = [.. CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand.Name)];
                _menus.Add((fieldName, names, defaultValue));
                return _select(fieldName, names);
            });
    }

    private void VerifyNothingSaved()
    {
        _parametersManager.Verify(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheCommandAndReloadsTheMenu()
    {
        // Act
        var sut = new ReadFromAutoinstallUserDataCliMenuCommand(_parametersManager.Object, InstallationName);

        // Assert
        Assert.Equal("Read Disk And Network From autoinstall-user-data...", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WithoutTheInstallation_ReturnsFalse()
    {
        // Arrange
        _parameters.ServerInstallations.Clear();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("[ERROR] Server Installation oslo not found", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task RunBody_WithStoredFile_TakesTheOnlyDiskAndInterfaceWithoutAsking()
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = WriteUserData(OneDiskAndEthernet);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_filePathPrompts);
        Assert.Empty(_menus);
        Assert.Equal("SERIAL1", _installation.InstallDiskSerial);
        Assert.Equal("enp3s0", _installation.NetworkInterfaceName);
        string output = _consoleOutput.ToString();
        Assert.Contains("InstallDiskSerial: SERIAL1", output, StringComparison.Ordinal);
        Assert.Contains("NetworkInterfaceName: enp3s0", output, StringComparison.Ordinal);
        _parametersManager.Verify(x => x.Save(_parameters, "Server Installation oslo updated", It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunBody_WhenTheSaveFails_ReturnsFalse()
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = WriteUserData(OneDiskAndEthernet);
        _saveResult = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task RunBody_WithoutStoredFile_AsksForItAndKeepsIt(string? storedPath)
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = storedPath;
        _filePathAnswer = WriteUserData(OneDiskAndEthernet);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([(nameof(ServerInstallationModel.AutoinstallUserDataFilePath), storedPath)], _filePathPrompts);
        Assert.Equal(_filePathAnswer, _installation.AutoinstallUserDataFilePath);
        Assert.Equal("SERIAL1", _installation.InstallDiskSerial);
    }

    //a stored path whose file is gone is offered as the default of the question
    [Fact]
    public async Task RunBody_WithMissingStoredFileAndNoAnswer_ReturnsFalse()
    {
        // Arrange
        string missingPath = Path.Combine(_folder, "missing");
        _installation.AutoinstallUserDataFilePath = missingPath;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal([(nameof(ServerInstallationModel.AutoinstallUserDataFilePath), (string?)missingPath)],
            _filePathPrompts);
        Assert.Contains("[ERROR] File  not found", _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task RunBody_WithMissingAnsweredFile_ReturnsFalse()
    {
        // Arrange
        _filePathAnswer = Path.Combine(_folder, "missing");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains($"[ERROR] File {_filePathAnswer} not found", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.Null(_installation.AutoinstallUserDataFilePath);
        VerifyNothingSaved();
    }

    //the disks are shown with their path, and the disk of the installation is the default
    [Fact]
    public async Task RunBody_WithTwoDisks_OffersThemWithTheCurrentDiskAsTheDefault()
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = WriteUserData(TwoDisksAndEthernets);
        _installation.InstallDiskSerial = "SERIAL2";

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        (string fieldName, List<string> names, string? defaultValue) = _menus[0];
        Assert.Equal("Install Disk", fieldName);
        Assert.Equal(["SERIAL1 /dev/nvme0n1", "SERIAL2"], names);
        Assert.Equal("SERIAL2", defaultValue);
        Assert.Equal("SERIAL1", _installation.InstallDiskSerial);
        Assert.Contains("InstallDiskSerial: SERIAL1", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WithTwoDisksAndNoCurrentDisk_OffersNoDefault()
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = WriteUserData(TwoDisksAndEthernets);
        _select = (_, names) => names[1];

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Null(_menus[0].DefaultValue);
        Assert.Equal("SERIAL2", _installation.InstallDiskSerial);
    }

    [Fact]
    public async Task RunBody_WhenTheChoiceIsNotADisk_KeepsTheSerial()
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = WriteUserData(TwoDisksAndEthernets);
        _installation.InstallDiskSerial = "OLD";
        _select = (fieldName, names) => fieldName == "Install Disk" ? "something else" : names[0];

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal("OLD", _installation.InstallDiskSerial);
        Assert.DoesNotContain("InstallDiskSerial:", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WithTwoEthernets_OffersThemWithTheCurrentInterfaceAsTheDefault()
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = WriteUserData(TwoDisksAndEthernets);
        _installation.NetworkInterfaceName = "enp4s0";
        _select = (_, names) => names[^1];

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        (string fieldName, List<string> names, string? defaultValue) = _menus[1];
        Assert.Equal("Network Interface", fieldName);
        Assert.Equal(["enp3s0", "enp4s0"], names);
        Assert.Equal("enp4s0", defaultValue);
        Assert.Equal("enp4s0", _installation.NetworkInterfaceName);
        Assert.Contains("NetworkInterfaceName: enp4s0", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenNoInterfaceIsChosen_KeepsTheInterface()
    {
        // Arrange
        _installation.AutoinstallUserDataFilePath = WriteUserData(TwoDisksAndEthernets);
        _installation.NetworkInterfaceName = "eth0";
        _select = (fieldName, names) => fieldName == "Network Interface" ? null : names[0];

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal("eth0", _installation.NetworkInterfaceName);
        Assert.DoesNotContain("NetworkInterfaceName:", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the file path is still kept, so the next read does not ask again
    [Fact]
    public async Task RunBody_WithoutDisksAndEthernets_WarnsAndKeepsTheValues()
    {
        // Arrange
        string path = WriteUserData("autoinstall:\n  version: 1\n");
        _installation.AutoinstallUserDataFilePath = path;
        _installation.InstallDiskSerial = "OLD";
        _installation.NetworkInterfaceName = "eth0";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal("OLD", _installation.InstallDiskSerial);
        Assert.Equal("eth0", _installation.NetworkInterfaceName);
        string output = _consoleOutput.ToString();
        Assert.Contains("[warning] No Install Disk found in the file", output, StringComparison.Ordinal);
        Assert.Contains("[warning] No Network Interface found in the file", output, StringComparison.Ordinal);
    }

    //the test host has no console input, so reading the path fails right after the question is printed
    [Fact]
    public void InputFilePath_OnTheConsole_AsksForTheField()
    {
        // Act
        Assert.Throws<InvalidOperationException>(() =>
            ReadFromAutoinstallUserDataCliMenuCommand.InputFilePath("AutoinstallUserDataFilePath", null));

        // Assert
        Assert.Contains("Enter AutoinstallUserDataFilePath: ", _consoleOutput.ToString(), StringComparison.Ordinal);
    }
}
