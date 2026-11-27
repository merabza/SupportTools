using System.Collections.Generic;

namespace SupportTools.Menu.ServerInstallations;

public sealed record AutoinstallUserData(
    List<AutoinstallUserDataDisk> Disks,
    List<string> EthernetNames,
    List<AutoinstallUserDataBond> Bonds);
