using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ServerInstallations;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ServerInstallations;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class GenerateServerInstallationFilesCliMenuCommandTests : IDisposable
{
    private const string InstallationName = "oslo";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), $"GenerateServerInstallationFilesTests-{Guid.NewGuid():N}");
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public GenerateServerInstallationFilesCliMenuCommandTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private static ServerInstallationModel CreateValidInstallation()
    {
        return new ServerInstallationModel
        {
            Hostname = "oslo",
            UserName = "merab",
            PasswordHash = "$6$salt$hash",
            NetworkInterfaceName = "enp3s0",
            InstallDiskSerial = "SERIAL"
        };
    }

    private GenerateServerInstallationFilesCliMenuCommand CreateSut()
    {
        return new GenerateServerInstallationFilesCliMenuCommand(_parametersManager.Object, InstallationName);
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheCommandAndReloadsTheMenu()
    {
        // Act
        GenerateServerInstallationFilesCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Generate grub.cfg And autoinstall.yaml", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WithoutTheInstallation_ReturnsFalse()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("[ERROR] Server Installation oslo not found", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //every error is shown at once, so the installation can be fixed with one edit
    [Fact]
    public async Task RunBody_WithInvalidInstallation_ShowsEveryErrorAndWritesNothing()
    {
        // Arrange
        _parameters.ServerInstallations[InstallationName] = new ServerInstallationModel { FilesFolder = _folder };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        Assert.Contains("[ERROR] Hostname is required", output, StringComparison.Ordinal);
        Assert.Contains("[ERROR] InstallDiskSerial is required", output, StringComparison.Ordinal);
        Assert.False(Directory.Exists(_folder));
    }

    //the files folder of the installation wins over the work folder
    [Fact]
    public async Task RunBody_WithFilesFolder_WritesBothFilesThere()
    {
        // Arrange
        ServerInstallationModel installation = CreateValidInstallation();
        installation.FilesFolder = Path.Combine(_folder, "files");
        _parameters.WorkFolder = Path.Combine(_folder, "work");
        _parameters.ServerInstallations[InstallationName] = installation;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        string grubCfgPath = Path.Combine(_folder, "files", "grub.cfg");
        string autoinstallPath = Path.Combine(_folder, "files", "autoinstall.yaml");
        Assert.Equal(ServerInstallationFilesGenerator.CreateGrubCfg(installation),
            await File.ReadAllTextAsync(grubCfgPath));
        Assert.Equal(ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation),
            await File.ReadAllTextAsync(autoinstallPath));
        Assert.False(Directory.Exists(Path.Combine(_folder, "work")));
        string output = _consoleOutput.ToString();
        Assert.Contains($"Created {grubCfgPath}", output, StringComparison.Ordinal);
        Assert.Contains($"Created {autoinstallPath}", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task RunBody_WithoutFilesFolder_WritesIntoTheWorkFolder(string? filesFolder)
    {
        // Arrange
        ServerInstallationModel installation = CreateValidInstallation();
        installation.FilesFolder = filesFolder;
        _parameters.WorkFolder = _folder;
        _parameters.ServerInstallations[InstallationName] = installation;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(Path.Combine(_folder, "ServerInstallations", InstallationName, "grub.cfg")));
        Assert.True(File.Exists(Path.Combine(_folder, "ServerInstallations", InstallationName, "autoinstall.yaml")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task RunBody_WithoutFilesFolderAndWorkFolder_ReturnsFalse(string? workFolder)
    {
        // Arrange
        _parameters.WorkFolder = workFolder;
        _parameters.ServerInstallations[InstallationName] = CreateValidInstallation();

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("[ERROR] FilesFolder and WorkFolder are empty", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }
}
