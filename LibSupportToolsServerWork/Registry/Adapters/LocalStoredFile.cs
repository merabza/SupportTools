using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Adapters;

//საიდუმლო ფაილის წაკითხვა და ჩაწერა (C6). სერვერი შიგთავსს ტექსტად ინახავს და ჰეშს მის UTF-8 ბაიტებზე ითვლის (B8),
//ამიტომ სინქრონიზდება მხოლოდ UTF-8 ტექსტი (BOM-ით ან მის გარეშე), StsStoredFileDataModel.ContentMaxBytes-მდე.
//BOM შიგთავსის ნაწილი არ არის: ჩაწერისას არსებული ფაილის BOM რჩება, ახალი ფაილი კი BOM-ის გარეშე იწერება. შიგთავსი
//არსად იბეჭდება: პრობლემის აღწერაში მხოლოდ მიზეზი წერია
internal static class LocalStoredFile
{
    private const string NotTextProblem = "is not a UTF-8 text file";

    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private static readonly string TooLargeProblem = $"is larger than {StsStoredFileDataModel.ContentMaxBytes} bytes";

    //მკაცრი დეკოდერი: არასწორი UTF-8 ბაიტები DecoderFallbackException-ს ისვრის, შიგთავსი ჩუმად არ იცვლება
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    //ფაილის შიგთავსი და ჰეში, ან პრობლემა (წინადადების გაგრძელება ფაილის გზის შემდეგ), რის გამოც ფაილი არ სინქრონიზდება:
    //ზღვარზე დიდია, ბინარულია (NUL ბაიტი ან არასწორი UTF-8) ან ვერ იკითხება
    public static (LocalStoredFileContent? Content, string? Problem) Read(string localPath)
    {
        byte[] bytes;
        try
        {
            //ზღვარზე დიდი ფაილი მთლიანად არ იკითხება
            if (new FileInfo(localPath).Length > StsStoredFileDataModel.ContentMaxBytes + Utf8Bom.Length)
            {
                return (null, TooLargeProblem);
            }

            bytes = File.ReadAllBytes(localPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return (null, $"cannot be read ({e.Message})");
        }

        ReadOnlySpan<byte> contentBytes = bytes.AsSpan(HasBom(bytes) ? Utf8Bom.Length : 0);
        if (contentBytes.Length > StsStoredFileDataModel.ContentMaxBytes)
        {
            return (null, TooLargeProblem);
        }

        if (contentBytes.Contains((byte)0))
        {
            return (null, NotTextProblem);
        }

        string content;
        try
        {
            content = StrictUtf8.GetString(contentBytes);
        }
        catch (DecoderFallbackException)
        {
            return (null, NotTextProblem);
        }

        return (
            new LocalStoredFileContent(content, Convert.ToHexString(SHA256.HashData(contentBytes)),
                contentBytes.Length), null);
    }

    //ჩაწერა საჭირო ფოლდერების შექმნით. არსებული ფაილის BOM რჩება, რომ ფაილი მხოლოდ შიგთავსით შეიცვალოს
    public static void Write(string localPath, string content)
    {
        string? folder = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        bool keepBom = File.Exists(localPath) && HasBom(File.ReadAllBytes(localPath));
        File.WriteAllText(localPath, content, new UTF8Encoding(keepBom));
    }

    private static bool HasBom(byte[] bytes)
    {
        return bytes.AsSpan().StartsWith(Utf8Bom);
    }
}
