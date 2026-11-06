using System;
using System.IO;
using System.Text;

namespace LibGitWork.OutdatedPackages;

//git-ის Directory.Packages.props-ის შედარება dotnet outdated-ის გაშვებამდე და მის შემდეგ
internal static class CentralPackageVersionsFile
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

    private static bool StartsWithBom(byte[] bytes)
    {
        return bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble);
    }
}
