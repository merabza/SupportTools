using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ServerInstallations;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ServerInstallations;

//two temporary folders stand for the roots of two USB drives
[Collection(ConsoleCaptureCollection.Name)]
public sealed class WriteServerInstallationToUsbCliMenuCommandTests : IDisposable
{
    private const string FirstDescription = @"E:\ FIRST (Removable, 7.5 GB)";
    private const string InstallationName = "oslo";
    private const string SecondDescription = @"F:\ SECOND (Removable, 14.9 GB)";

    private readonly List<string> _confirmQuestions = [];
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<InstallationUsbDrive> _drives;
    private readonly string _firstRoot;
    private readonly ServerInstallationModel _installation = new()
    {
        Hostname = "oslo",
        UserName = "merab",
        PasswordHash = "$6$salt$hash",
        NetworkInterfaceName = "enp3s0",
        InstallDiskSerial = "SERIAL"
    };
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"WriteToUsbTests-{Guid.NewGuid():N}");
    private readonly string _secondRoot;
    private bool _confirmAnswer;
    private int _findDrivesCalls;
    private string? _menuDefault;
    private string? _menuFieldName;
    private List<string>? _menuItemNames;
    private Func<List<string>, string?> _select = x => x[1];

    public WriteServerInstallationToUsbCliMenuCommandTests()
    {
        _firstRoot = CreateInstallationMedia("first");
        _secondRoot = CreateInstallationMedia("second");
        _drives =
        [
            new InstallationUsbDrive(_firstRoot, FirstDescription),
            new InstallationUsbDrive(_secondRoot, SecondDescription)
        ];
        _parameters.ServerInstallations[InstallationName] = _installation;
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_root, true);
    }

    private string CreateInstallationMedia(string name)
    {
        string root = Path.Combine(_root, name);
        Directory.CreateDirectory(Path.Combine(root, "boot", "grub"));
        Directory.CreateDirectory(Path.Combine(root, "casper"));
        File.WriteAllText(Path.Combine(root, "boot", "grub", "grub.cfg"), "set timeout=30\n");
        return root;
    }

    private WriteServerInstallationToUsbCliMenuCommand CreateSut()
    {
        return new WriteServerInstallationToUsbCliMenuCommand(_parametersManager.Object, InstallationName, () =>
        {
            _findDrivesCalls++;
            return _drives;
        }, (fieldName, menuSet, defaultValue) =>
        {
            _menuFieldName = fieldName;
            _menuDefault = defaultValue;
            _menuItemNames = [.. CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand.Name)];
            return _select(_menuItemNames);
        }, question =>
        {
            _confirmQuestions.Add(question);
            return _confirmAnswer;
        });
    }

    private static bool HasAutoinstall(string root)
    {
        return File.Exists(Path.Combine(root, "autoinstall.yaml"));
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheCommandAndReloadsTheMenu()
    {
        // Act
        var sut = new WriteServerInstallationToUsbCliMenuCommand(_parametersManager.Object, InstallationName);

        // Assert
        Assert.Equal("Write To Installation USB...", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WithInvalidInstallation_LooksForNoDrive()
    {
        // Arrange
        _installation.Hostname = null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(0, _findDrivesCalls);
        Assert.Contains("[ERROR] Hostname is required", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WithoutInstallationMedia_ReturnsFalse()
    {
        // Arrange
        _drives.Clear();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Null(_menuItemNames);
        Assert.Contains(
            @"[ERROR] No Ubuntu installation USB found (a local or RDP redirected drive with boot\grub\grub.cfg and a casper folder)",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WithInstallationMedia_OffersThemByTheirDescriptions()
    {
        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal("Installation USB", _menuFieldName);
        Assert.Null(_menuDefault);
        Assert.Equal([FirstDescription, SecondDescription], _menuItemNames);
    }

    [Fact]
    public async Task RunBody_WhenTheChoiceIsNotADrive_AsksNothingAndWritesNothing()
    {
        // Arrange
        _select = _ => null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_confirmQuestions);
        Assert.False(HasAutoinstall(_firstRoot));
        Assert.False(HasAutoinstall(_secondRoot));
    }

    //the drive is erased on the computer that boots from it, so the warning names the disk that will be erased
    [Fact]
    public async Task RunBody_WhenNotConfirmed_WarnsAndWritesNothing()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal([$"Write grub.cfg and autoinstall.yaml of oslo to {_secondRoot}?"], _confirmQuestions);
        Assert.Contains(
            $"A computer booted from {_secondRoot} installs Ubuntu without asking and erases the disk with serial SERIAL.",
            _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.False(HasAutoinstall(_secondRoot));
    }

    [Fact]
    public async Task RunBody_WhenConfirmed_WritesTheFilesToTheChosenDriveOnly()
    {
        // Arrange
        _confirmAnswer = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.True(HasAutoinstall(_secondRoot));
        Assert.False(HasAutoinstall(_firstRoot));
        Assert.Contains($"Written to {_secondRoot}. Remove autoinstall.yaml from the USB after the installation",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the test host has no console input, so reading the answer fails right after the question is printed
    [Fact]
    public void Confirm_OnTheConsole_AsksWithNoAsTheDefaultAnswer()
    {
        // Act
        Assert.Throws<InvalidOperationException>(() => WriteServerInstallationToUsbCliMenuCommand.Confirm("Write?"));

        // Assert
        Assert.Contains("Write? (y/n)[n]: ", _consoleOutput.ToString(), StringComparison.Ordinal);
    }
}
