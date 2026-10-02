using System;
using System.Collections.Generic;
using System.IO;
using SupportToolsData.Models;

namespace LibSupportToolsServerWork.Registry.Paths;

//გზების გარდაქმნა კანონიკურ და ლოკალურ ფორმებს შორის (README §4.4, G3). კანონიკური ფორმა Windows-ის აბსოლუტური
//გზაა, როგორც მთავარ კომპიუტერზე: D:\1WorkDotnet\X\Y.slnx. ToLocal წესის (PathMappingModel) კანონიკურ prefix-ს
//ლოკალური prefix-ით ცვლის, ToCanonical კი პირიქით. რამდენიმე შესაფერისი წესიდან იმარჯვებს ყველაზე გრძელი prefix-ი.
//დამთხვევა მხოლოდ საზღვარზე ითვლება: D:\1WorkDotnet ემთხვევა D:\1WorkDotnet\X-ს, მაგრამ არა D:\1WorkDotnetX-ს.
//prefix-ები რეგისტრის გარეშე შედარდება და ბოლო გამყოფი არ ითვლება. Windows-ის გარდა სხვა OS-ზე დარჩენილ ნაწილში
//გამყოფებიც იცვლება. Windows-ზე, რომელსაც იგივე განლაგება აქვს, წესები არ სჭირდება და არაფერი იცვლება
public sealed class PathMapper
{
    private const char CanonicalSeparator = '\\';
    private const char ForwardSlash = '/';

    private readonly List<PathMappingIssue> _issues = [];
    private readonly List<(string CanonicalPrefix, string LocalPrefix)> _rules = [];

    //true, როცა ლოკალური გამყოფი '/'-ია, ანუ OS Windows არ არის
    private readonly bool _usesForwardSlash;

    public PathMapper(IEnumerable<PathMappingModel> pathMappings) : this(pathMappings, Path.DirectorySeparatorChar)
    {
    }

    //ლოკალური გამყოფი პარამეტრადაა გამოტანილი, რომ ტესტებმა Linux-ის რეჟიმიც Windows-ზე შეამოწმონ
    internal PathMapper(IEnumerable<PathMappingModel> pathMappings, char localSeparator)
    {
        _usesForwardSlash = localSeparator == ForwardSlash;

        foreach (PathMappingModel pathMapping in pathMappings)
        {
            //არასრული წესი არაფერს გარდაქმნის
            if (string.IsNullOrWhiteSpace(pathMapping.CanonicalPrefix) ||
                string.IsNullOrWhiteSpace(pathMapping.LocalPrefix))
            {
                continue;
            }

            //კანონიკური ფორმის გამყოფი ყოველთვის '\'-ია
            _rules.Add((ToComparable(NormalizePrefix(pathMapping.CanonicalPrefix)),
                NormalizePrefix(pathMapping.LocalPrefix)));
        }
    }

    //გზები, რომლებსაც არცერთი წესი არ დაემთხვა და უცვლელად დარჩა. სინქრონიზაციის ბრძანება (C5) ამ სიას აჩვენებს
    public IReadOnlyList<PathMappingIssue> Issues => _issues;

    public string? ToLocal(string? canonicalPath)
    {
        return string.IsNullOrEmpty(canonicalPath) ? canonicalPath : Map(canonicalPath, EPathMappingDirection.ToLocal);
    }

    public string? ToCanonical(string? localPath)
    {
        return string.IsNullOrEmpty(localPath) ? localPath : Map(localPath, EPathMappingDirection.ToCanonical);
    }

    //შეფარდებითი გზა (მაგ. GitProjectFolderName) prefix-ით არ გარდაიქმნება: Linux-ზე მხოლოდ გამყოფები იცვლება
    public string? NormalizeRelativeToLocal(string? relativePath)
    {
        return _usesForwardSlash ? relativePath?.Replace(CanonicalSeparator, ForwardSlash) : relativePath;
    }

    public string? NormalizeRelativeToCanonical(string? relativePath)
    {
        return _usesForwardSlash ? relativePath?.Replace(ForwardSlash, CanonicalSeparator) : relativePath;
    }

    //ორი prefix-ი ერთნაირია, თუ მხოლოდ რეგისტრით, ბოლო გამყოფით ან გამყოფის სახით განსხვავდება. null ცარიელად ითვლება
    public static bool IsSamePrefix(string? first, string? second)
    {
        return string.Equals(ToComparable(NormalizePrefix(first ?? string.Empty)),
            ToComparable(NormalizePrefix(second ?? string.Empty)), StringComparison.OrdinalIgnoreCase);
    }

    //Windows-ის ფესვიანი გზა: დისკით (D:\...) ან ქსელური (\\server\share\...)
    public static bool IsWindowsRooted(string path)
    {
        return path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':' ||
               path.StartsWith(@"\\", StringComparison.Ordinal);
    }

    private string Map(string path, EPathMappingDirection direction)
    {
        bool toLocal = direction == EPathMappingDirection.ToLocal;

        (string From, string To)? bestRule = null;
        foreach ((string canonicalPrefix, string localPrefix) in _rules)
        {
            string from = toLocal ? canonicalPrefix : localPrefix;
            if ((bestRule is null || from.Length > bestRule.Value.From.Length) && StartsWithPrefix(path, from))
            {
                bestRule = (from, toLocal ? localPrefix : canonicalPrefix);
            }
        }

        if (bestRule is null)
        {
            //Windows-ზე დაუმთხვეველი გზა ისედაც Windows-ის ფორმისაა. Linux-ზე კი Windows-ის ფესვიანი გზა ლოკალურად
            //ვერ იმუშავებს, Linux-ის ფესვიანი გზა კი სერვერზე კანონიკური ფორმის გარეშე მოხვდებოდა
            if (_usesForwardSlash && (toLocal ? IsWindowsRooted(path) : path[0] == ForwardSlash))
            {
                AddIssue(new PathMappingIssue(direction, path));
            }

            return path;
        }

        string remainder = path[bestRule.Value.From.Length..];
        if (_usesForwardSlash)
        {
            remainder = toLocal
                ? remainder.Replace(CanonicalSeparator, ForwardSlash)
                : remainder.Replace(ForwardSlash, CanonicalSeparator);
        }

        return bestRule.Value.To + remainder;
    }

    //ერთი და იგივე გზა რამდენიმე ველში შეიძლება შეგვხვდეს, სიაში კი ერთხელ ჩანს
    private void AddIssue(PathMappingIssue issue)
    {
        if (!_issues.Contains(issue))
        {
            _issues.Add(issue);
        }
    }

    //prefix-ი გზის დასაწყისს ემთხვევა რეგისტრის გარეშე და მხოლოდ საზღვარზე: prefix-ის შემდეგ გზა ან მთავრდება, ან
    //გამყოფი მოდის. '\' და '/' ერთნაირად ითვლება
    private static bool StartsWithPrefix(string path, string prefix)
    {
        return path.Length >= prefix.Length &&
               string.Equals(ToComparable(path[..prefix.Length]), ToComparable(prefix),
                   StringComparison.OrdinalIgnoreCase) && (path.Length == prefix.Length ||
                                                           path[prefix.Length] is CanonicalSeparator or ForwardSlash);
    }

    //prefix-ის ბოლო გამყოფი არ ითვლება: d:\1WorkDotnet\ იგივეა, რაც D:\1WorkDotnet
    private static string NormalizePrefix(string prefix)
    {
        return prefix.Trim().TrimEnd(CanonicalSeparator, ForwardSlash);
    }

    private static string ToComparable(string path)
    {
        return path.Replace(ForwardSlash, CanonicalSeparator);
    }
}
