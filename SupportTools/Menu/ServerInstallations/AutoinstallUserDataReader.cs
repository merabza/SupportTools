using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace SupportTools.Menu.ServerInstallations;

public sealed record AutoinstallUserDataDisk(string Serial, string? Path);

public sealed record AutoinstallUserDataBond(string Name, List<string> InterfaceNames, string? Mode);

public sealed record AutoinstallUserData(
    List<AutoinstallUserDataDisk> Disks,
    List<string> EthernetNames,
    List<AutoinstallUserDataBond> Bonds);

//წინა ინსტალაციის /var/log/installer/autoinstall-user-data ფაილიდან იმ კომპიუტერის მონაცემები იკითხება, რომლებიც
//ხელით ძნელი გასარკვევია: დისკების serial-ები (storage.config, type: disk), ქსელის ინტერფეისების სახელები და
//bond-ები (მათი ინტერფეისები და რეჟიმი)
public static class AutoinstallUserDataReader
{
    public static AutoinstallUserData Read(string yamlText)
    {
        var yaml = new YamlStream();
        yaml.Load(new StringReader(yamlText));
        if (yaml.Documents.Count == 0 || yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            return new AutoinstallUserData([], [], []);
        }

        //ფაილი #cloud-config-ია და პარამეტრები autoinstall-შია. ძველ ფორმატში ისინი პირდაპირ ძირშია
        YamlMappingNode autoinstall = GetChild(root, "autoinstall") as YamlMappingNode ?? root;

        List<AutoinstallUserDataDisk> disks = [];
        if (GetChild(GetChild(autoinstall, "storage"), "config") is YamlSequenceNode storageConfig)
        {
            disks.AddRange(storageConfig.Children.OfType<YamlMappingNode>()
                .Where(x => GetScalar(x, "type") == "disk" && !string.IsNullOrWhiteSpace(GetScalar(x, "serial")))
                .Select(x => new AutoinstallUserDataDisk(GetScalar(x, "serial")!, GetScalar(x, "path"))));
        }

        //network შეიძლება ერთხელ კიდევ network-ში იყოს ჩადგმული (network: {network: {version: 2, ...}})
        YamlNode? network = GetChild(autoinstall, "network");
        YamlNode? ethernets = GetChild(network, "ethernets") ?? GetChild(GetChild(network, "network"), "ethernets");
        List<string> ethernetNames = ethernets is YamlMappingNode ethernetsMapping
            ? [.. ethernetsMapping.Children.Keys.OfType<YamlScalarNode>().Select(x => x.Value).OfType<string>()]
            : [];

        YamlNode? bonds = GetChild(network, "bonds") ?? GetChild(GetChild(network, "network"), "bonds");
        List<AutoinstallUserDataBond> bondList = bonds is YamlMappingNode bondsMapping
            ? [.. bondsMapping.Children.Select(x => ReadBond(x.Key, x.Value)).OfType<AutoinstallUserDataBond>()]
            : [];

        return new AutoinstallUserData(disks, ethernetNames, bondList);
    }

    private static AutoinstallUserDataBond? ReadBond(YamlNode key, YamlNode bond)
    {
        if (key is not YamlScalarNode { Value: { } name })
        {
            return null;
        }

        List<string> interfaceNames = GetChild(bond, "interfaces") is YamlSequenceNode interfaces
            ? [.. interfaces.Children.OfType<YamlScalarNode>().Select(x => x.Value).OfType<string>()]
            : [];
        return new AutoinstallUserDataBond(name, interfaceNames, GetScalar(GetChild(bond, "parameters"), "mode"));
    }

    private static YamlNode? GetChild(YamlNode? node, string key)
    {
        return node is YamlMappingNode mapping && mapping.Children.TryGetValue(new YamlScalarNode(key),
            out YamlNode? child)
            ? child
            : null;
    }

    private static string? GetScalar(YamlNode? node, string key)
    {
        return (GetChild(node, key) as YamlScalarNode)?.Value;
    }
}
