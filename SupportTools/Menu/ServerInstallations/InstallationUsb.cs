using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SupportToolsData.Models;

namespace SupportTools.Menu.ServerInstallations;

public sealed record InstallationUsbDrive(string RootPath, string Description);

//Ubuntu Server-ის საინსტალაციო ფლეშკა (Rufus, "ISO Image mode", FAT32): grub.cfg იწერება boot\grub-ში,
//autoinstall.yaml კი ძირში, სადაც ინსტალერი მას თვითონ პოულობს
public static class InstallationUsb
{
    private const string OriginalGrubCfgFileName = "grub.cfg.original";

    //დისკები, რომლებიც Ubuntu-ს საინსტალაციო მედიას ჰგავს: აქვს boot\grub\grub.cfg და casper ფოლდერი.
    //RDP სესიაში კლიენტის კომპიუტერის გადამისამართებული დისკებიც მოწმდება (\\tsclient\E\)
    public static List<InstallationUsbDrive> FindDrives()
    {
        return
        [
            .. DriveInfo.GetDrives().Where(x => x.IsReady && IsInstallationMedia(x.RootDirectory.FullName))
                .Select(x => new InstallationUsbDrive(x.RootDirectory.FullName, GetDescription(x))),
            .. GetRdpDriveRoots(Environment.GetEnvironmentVariable("SESSIONNAME")).Where(IsInstallationMedia)
                .Select(x => new InstallationUsbDrive(x, $"{x} (RDP redirected drive)"))
        ];
    }

    //RDP-ით გადამისამართებული დისკები DriveInfo.GetDrives-ში არ ჩანს, მხოლოდ \\tsclient\<ასო>-ით მიიწვდომება.
    //RDP სესიის გარეთ \\tsclient-ის ძებნა ქსელის ტაიმაუტს ელოდება, ამიტომ მოწმდება მხოლოდ RDP სესიაში (SESSIONNAME = RDP-Tcp#N)
    internal static List<string> GetRdpDriveRoots(string? sessionName)
    {
        if (sessionName is null || !sessionName.StartsWith("RDP-", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return [.. Enumerable.Range('A', 26).Select(x => $@"\\tsclient\{(char)x}\")];
    }

    internal static string GetDescription(DriveInfo drive)
    {
        string size = (drive.TotalSize / 1024.0 / 1024 / 1024).ToString("0.0", CultureInfo.InvariantCulture);
        return $"{drive.RootDirectory.FullName} {drive.VolumeLabel} ({drive.DriveType}, {size} GB)";
    }

    internal static bool IsInstallationMedia(string rootPath)
    {
        return File.Exists(GetGrubCfgPath(rootPath)) && Directory.Exists(Path.Combine(rootPath, "casper"));
    }

    //ISO-ს თავდაპირველი grub.cfg ერთხელ ინახება grub.cfg.original-ად, რომ ფლეშკა ჩვეულებრივ მედიად დაბრუნდეს
    internal static void Write(string rootPath, ServerInstallationModel installation)
    {
        string grubCfgPath = GetGrubCfgPath(rootPath);
        string originalPath = Path.Combine(Path.GetDirectoryName(grubCfgPath)!, OriginalGrubCfgFileName);
        if (!File.Exists(originalPath))
        {
            File.Copy(grubCfgPath, originalPath);
        }

        WriteLinuxText(grubCfgPath, ServerInstallationFilesGenerator.CreateGrubCfg(installation));
        WriteLinuxText(Path.Combine(rootPath, ServerInstallationFilesGenerator.AutoinstallFileName),
            ServerInstallationFilesGenerator.CreateAutoinstallYaml(installation));
    }

    //ფაილები BOM-ის გარეშე იწერება: grub და cloud-init BOM-ს ტექსტად აღიქვამდნენ
    internal static void WriteLinuxText(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static string GetGrubCfgPath(string rootPath)
    {
        return Path.Combine(rootPath, "boot", "grub", ServerInstallationFilesGenerator.GrubCfgFileName);
    }
}
