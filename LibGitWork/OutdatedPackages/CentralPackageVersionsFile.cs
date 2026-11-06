using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LibGitWork.OutdatedPackages;

//git-ის Directory.Packages.props-ის შედარება dotnet outdated-ის გაშვებამდე და მის შემდეგ
internal static partial class CentralPackageVersionsFile
{
    public const string FileName = "Directory.Packages.props";

    //ფაილის ბაიტები; null, თუ ფაილი არ არსებობს
    public static byte[]? ReadBytes(string filePath)
    {
        return File.Exists(filePath) ? File.ReadAllBytes(filePath) : null;
    }

    //dotnet outdated -n ვერსიებს თავად წერს File.WriteAllText-ით, რომელიც UTF-8-ის BOM-ს შლის. თუ ფაილს გაშვებამდე BOM
    //ჰქონდა, ის ბრუნდება, რომ commit-ში ვერსიების გარდა ფაილის პირველი სტრიქონიც არ შეიცვალოს
    public static void RestoreLostBom(string filePath, byte[]? bytesBefore)
    {
        if (bytesBefore is null || !StartsWithBom(bytesBefore) || !File.Exists(filePath))
        {
            return;
        }

        byte[] bytes = File.ReadAllBytes(filePath);
        if (StartsWithBom(bytes))
        {
            return;
        }

        File.WriteAllBytes(filePath, [.. Encoding.UTF8.GetPreamble(), .. bytes]);
    }

    //შეიცვალა თუ არა ფაილი გაშვებამდელთან შედარებით. BOM-ის აღდგენის შემდეგ ცვლილება მხოლოდ ვერსიებია
    public static bool HasChanged(string filePath, byte[]? bytesBefore)
    {
        byte[]? bytes = ReadBytes(filePath);
        if (bytes is null || bytesBefore is null)
        {
            return bytes is not null || bytesBefore is not null;
        }

        return !bytes.AsSpan().SequenceEqual(bytesBefore);
    }

    //რამდენი პაკეტის ვერსია შეიცვალა (ან დაემატა). ჩანაწერები იმავე ფორმატით იკითხება, რომლითაც dotnet outdated -n
    //მათ წერს, პაკეტის სახელი რეგისტრის გარეშე
    public static int CountChangedVersions(byte[]? bytesBefore, byte[]? bytesAfter)
    {
        Dictionary<string, string> versionsBefore = ReadVersions(bytesBefore);
        return ReadVersions(bytesAfter).Count(x =>
            !versionsBefore.TryGetValue(x.Key, out string? versionBefore) || versionBefore != x.Value);
    }

    private static Dictionary<string, string> ReadVersions(byte[]? bytes)
    {
        var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (bytes is null)
        {
            return versions;
        }

        foreach (Match match in PackageVersionRegex().Matches(Encoding.UTF8.GetString(bytes)))
        {
            versions[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return versions;
    }

    private static bool StartsWithBom(byte[] bytes)
    {
        return bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble);
    }

    [GeneratedRegex("""<(?:PackageVersion|GlobalPackageReference)\s*(?:Include|Update)="([^"]+)"\s*Version="([^"]*)""")]
    private static partial Regex PackageVersionRegex();
}
