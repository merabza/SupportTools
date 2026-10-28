using SupportTools.Menu.ServerInstallations;
using Xunit;

namespace SupportTools.Tests.Menu.ServerInstallations;

public sealed class AutoinstallUserDataReaderTests
{
    //the parts of /var/log/installer/autoinstall-user-data of a real installation that the reader uses
    private const string UserData = """
                                    #cloud-config
                                    autoinstall:
                                      network:
                                        ethernets:
                                          enp3s0:
                                            dhcp4: true
                                          enp4s0:
                                            dhcp4: true
                                        version: 2
                                      storage:
                                        config:
                                        - transport: pcie
                                          id: nvme-controller-nvme0
                                          type: nvme_controller
                                        - ptable: gpt
                                          serial: CT500P3PSSD8_2331E865DB19_1
                                          path: /dev/nvme0n1
                                          id: disk-nvme0n1
                                          type: disk
                                        - device: disk-nvme0n1
                                          serial: not-a-disk
                                          type: partition
                                        - serial: WDC_WD10EZEX
                                          id: disk-sda
                                          type: disk
                                      version: 1
                                    """;

    [Fact]
    public void Read_WithDisksAndEthernets_ReturnsDiskSerialsAndInterfaceNames()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read(UserData);

        // Assert
        Assert.Equal(
        [
            new AutoinstallUserDataDisk("CT500P3PSSD8_2331E865DB19_1", "/dev/nvme0n1"),
            new AutoinstallUserDataDisk("WDC_WD10EZEX", null)
        ], result.Disks);
        Assert.Equal(["enp3s0", "enp4s0"], result.EthernetNames);
    }

    [Fact]
    public void Read_WithNestedNetworkAndNoStorage_ReturnsInterfaceNamesOnly()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("""
                                                                    autoinstall:
                                                                      network:
                                                                        network:
                                                                          version: 2
                                                                          ethernets:
                                                                            eth0: {}
                                                                    """);

        // Assert
        Assert.Empty(result.Disks);
        Assert.Equal(["eth0"], result.EthernetNames);
    }

    //the nested form is read only when the usual one is missing
    [Fact]
    public void Read_WithUsualAndNestedEthernets_TakesTheUsualOnes()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("""
                                                                    autoinstall:
                                                                      network:
                                                                        ethernets:
                                                                          eth0: {}
                                                                        network:
                                                                          ethernets:
                                                                            eth9: {}
                                                                    """);

        // Assert
        Assert.Equal(["eth0"], result.EthernetNames);
    }

    [Fact]
    public void Read_WithEmptyText_ReturnsNothing()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read(string.Empty);

        // Assert
        Assert.Empty(result.Disks);
        Assert.Empty(result.EthernetNames);
    }

    [Fact]
    public void Read_WithScalarDocument_ReturnsNothing()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("just text");

        // Assert
        Assert.Empty(result.Disks);
        Assert.Empty(result.EthernetNames);
    }

    //older files have the parameters at the root, without the autoinstall key
    [Fact]
    public void Read_WithoutAutoinstallKey_ReadsTheRoot()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("""
                                                                    network:
                                                                      ethernets:
                                                                        eth0: {}
                                                                    storage:
                                                                      config:
                                                                      - serial: SERIAL
                                                                        type: disk
                                                                    """);

        // Assert
        Assert.Equal([new AutoinstallUserDataDisk("SERIAL", null)], result.Disks);
        Assert.Equal(["eth0"], result.EthernetNames);
    }

    //only disks with a serial can be matched by the installation
    [Fact]
    public void Read_WithDisksWithoutSerialAndOtherItems_SkipsThem()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("""
                                                                    autoinstall:
                                                                      storage:
                                                                        config:
                                                                        - plain item
                                                                        - id: disk-sda
                                                                          type: disk
                                                                        - serial: ' '
                                                                          type: disk
                                                                        - serial: [nested]
                                                                          type: disk
                                                                        - serial: SERIAL
                                                                          path: [nested]
                                                                          type: disk
                                                                    """);

        // Assert
        Assert.Equal([new AutoinstallUserDataDisk("SERIAL", null)], result.Disks);
    }

    [Fact]
    public void Read_WithStorageLayoutAndEthernetList_ReturnsNothing()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("""
                                                                    autoinstall:
                                                                      network:
                                                                        ethernets:
                                                                        - eth0
                                                                      storage:
                                                                        layout:
                                                                          name: lvm
                                                                    """);

        // Assert
        Assert.Empty(result.Disks);
        Assert.Empty(result.EthernetNames);
    }

    //the network of riga's Ubuntu 20.04 installation
    [Fact]
    public void Read_WithBond_ReturnsTheBondWithItsInterfacesAndMode()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("""
                                                                    autoinstall:
                                                                      network:
                                                                        bonds:
                                                                          bond0:
                                                                            addresses:
                                                                            - 192.168.10.8/24
                                                                            interfaces:
                                                                            - eno1
                                                                            - eno2
                                                                            parameters:
                                                                              mode: balance-rr
                                                                        ethernets:
                                                                          eno1: {}
                                                                          eno2: {}
                                                                        version: 2
                                                                    """);

        // Assert
        Assert.Equal(["eno1", "eno2"], result.EthernetNames);
        AutoinstallUserDataBond bond = Assert.Single(result.Bonds);
        Assert.Equal("bond0", bond.Name);
        Assert.Equal(["eno1", "eno2"], bond.InterfaceNames);
        Assert.Equal("balance-rr", bond.Mode);
    }

    [Fact]
    public void Read_WithNestedBondWithoutInterfacesAndParameters_ReturnsTheBondName()
    {
        // Act
        AutoinstallUserData result = AutoinstallUserDataReader.Read("""
                                                                    autoinstall:
                                                                      network:
                                                                        network:
                                                                          bonds:
                                                                            bond1: {}
                                                                    """);

        // Assert
        AutoinstallUserDataBond bond = Assert.Single(result.Bonds);
        Assert.Equal("bond1", bond.Name);
        Assert.Empty(bond.InterfaceNames);
        Assert.Null(bond.Mode);
    }
}
