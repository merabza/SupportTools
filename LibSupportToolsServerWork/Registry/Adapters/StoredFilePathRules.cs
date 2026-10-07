using System;
using System.Buffers;
using System.Linq;

namespace LibSupportToolsServerWork.Registry.Adapters;

//საიდუმლო ფაილის გზის წესი, როგორც სერვერზე (B8: SupportToolsServer-ის PathRules და StoredFile.PathMaxLength):
//კანონიკური Windows-ის აბსოლუტური გზა (X:\...) ერთადერთი გამყოფით (\). სეგმენტი ცარიელი არ არის, წერტილით ან ჰარით
//არ მთავრდება (ასე "." და ".." გამოირიცხება) და აკრძალულ სიმბოლოებს არ შეიცავს. სხვა გზას სერვერი არ იღებს, ამიტომ
//ასეთი ფაილი სინქრონიზაციაში არ მონაწილეობს
internal static class StoredFilePathRules
{
    //სერვერის სვეტის სიგრძე (StoredFile.PathMaxLength)
    public const int PathMaxLength = 400;

    private const char Separator = '\\';

    //Windows-ის აკრძალული სიმბოლოები; / აკრძალულია, რომ ერთ ფაილს ორი წერილობა არ ჰქონდეს
    private static readonly SearchValues<char> ForbiddenFileNameChars = SearchValues.Create("<>:\"/|?*");

    //სიგრძის ქვედა ზღვარი მხოლოდ path[2]-ის წაკითხვას იცავს: "X:\" სეგმენტის წესით გამოირიცხება (ცარიელი სეგმენტი)
    public static bool IsValidCanonicalPath(string path)
    {
        return path.Length is > 2 and <= PathMaxLength && char.IsAsciiLetter(path[0]) && path[1] == ':' &&
               path[2] == Separator && path[3..].Split(Separator).All(IsValidSegment);
    }

    private static bool IsValidSegment(string segment)
    {
        return segment.Length > 0 && !segment.EndsWith('.') && !segment.EndsWith(' ') &&
               !segment.AsSpan().ContainsAny(ForbiddenFileNameChars) && !segment.Any(char.IsControl);
    }
}
