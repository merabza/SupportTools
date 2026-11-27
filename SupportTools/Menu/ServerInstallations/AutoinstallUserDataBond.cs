using System.Collections.Generic;

namespace SupportTools.Menu.ServerInstallations;

public sealed record AutoinstallUserDataBond(string Name, List<string> InterfaceNames, string? Mode);
