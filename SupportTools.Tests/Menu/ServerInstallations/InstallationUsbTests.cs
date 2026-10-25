using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SupportTools.Menu.ServerInstallations;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ServerInstallations;

//a temporary folder stands for the root of the USB drive
public sealed class InstallationUsbTests : IDisposable
{
    private const string IsoGrubCfg = "set timeout=30\n";
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"InstallationUsbTests-{Guid.NewGuid():N}");

    public InstallationUsbTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "boot", "grub"));
        Directory.CreateDirectory(Path.Combine(_root, "casper"));
        File.WriteAllText(Path.Combine(_root, "boot", "grub", "grub.cfg"), IsoGrubCfg);
    }

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    private static ServerInstallationModel CreateInstallation()
    {
        return new ServerInstallationModel
        {
            Hostname = "test",
            UserName = "user",
            PasswordHash = "$6$salt$hash",
            NetworkInterfaceName = "eth0",
            InstallDiskSerial = "SERIAL"
        };
    }

    [Fact]
    public void IsInstallationMedia_WithGrubCfgAndCasper_ReturnsTrue()
    {
        // Act
        bool result = InstallationUsb.IsInstallationMedia(_root);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstallationMedia_WithoutCasper_ReturnsFalse()
    {
        // Arrange
        Directory.Delete(Path.Combine(_root, "casper"));

        // Act
        bool result = InstallationUsb.IsInstallationMedia(_root);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Write_TwiceToInstallationMedia_WritesBothFilesWithoutBomAndKeepsTheIsoGrubCfg()
    {
        // Arrange
        ServerInstallationModel installation = CreateInstallation();

        // Act
        InstallationUsb.Write(_root, installation);
        InstallationUsb.Write(_root, installation);

        // Assert
        Assert.Equal(IsoGrubCfg, File.ReadAllText(Path.Combine(_root, "boot", "grub", "grub.cfg.original")));
        Assert.Equal(ServerInstallationFilesGenerator.CreateGrubCfg(installation),
            File.ReadAllText(Path.Combine(_root, "boot", "grub", "grub.cfg")));
        byte[] autoinstall = File.ReadAllBytes(Path.Combine(_root, "autoinstall.yaml"));
        Assert.Equal((byte)'#', autoinstall[0]);
        Assert.Equal(ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation),
            File.ReadAllText(Path.Combine(_root, "autoinstall.yaml")));
    }

    [Fact]
    public void IsInstallationMedia_WithoutGrubCfg_ReturnsFalse()
    {
        // Arrange
        File.Delete(Path.Combine(_root, "boot", "grub", "grub.cfg"));

        // Act
        bool result = InstallationUsb.IsInstallationMedia(_root);

        // Assert
        Assert.False(result);
    }

    //the copy of the ISO's file is made once: a later write must not replace it with a generated grub.cfg
    [Fact]
    public void Write_WithAnOriginalCopy_KeepsIt()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_root, "boot", "grub", "grub.cfg.original"), "set timeout=10\n");

        // Act
        InstallationUsb.Write(_root, CreateInstallation());

        // Assert
        Assert.Equal("set timeout=10\n", File.ReadAllText(Path.Combine(_root, "boot", "grub", "grub.cfg.original")));
    }

    [Fact]
    public void WriteLinuxText_WhenCalled_WritesUtf8WithoutBom()
    {
        // Arrange
        string path = Path.Combine(_root, "text.txt");

        // Act
        InstallationUsb.WriteLinuxText(path, "ა\n");

        // Assert
        Assert.Equal([0xE1, 0x83, 0x90, (byte)'\n'], File.ReadAllBytes(path));
    }

    //the drives of this computer are not installation media, unless one really is plugged in
    [Fact]
    public void FindDrives_WhenCalled_ReturnsOnlyInstallationMedia()
    {
        // Act
        List<InstallationUsbDrive> result = InstallationUsb.FindDrives();

        // Assert
        Assert.All(result, x => Assert.True(InstallationUsb.IsInstallationMedia(x.RootPath)));
        Assert.All(result, x => Assert.StartsWith(x.RootPath, x.Description, StringComparison.Ordinal));
    }

    [Fact]
    public void GetDescription_ForADrive_ShowsItsRootTypeAndSize()
    {
        // Arrange
        var drive = new DriveInfo(Path.GetPathRoot(_root)!);

        // Act
        string result = InstallationUsb.GetDescription(drive);

        // Assert
        string gigabytes = (drive.TotalSize / 1073741824.0).ToString("0.0", CultureInfo.InvariantCulture);
        Assert.Equal($"{drive.RootDirectory.FullName} {drive.VolumeLabel} ({drive.DriveType}, {gigabytes} GB)", result);
    }
}
