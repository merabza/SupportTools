using System;
using System.Collections.Generic;
using System.Linq;

namespace LibSupportToolsServerWork.Registry.Mappers;

//კონტრაქტების ნორმალიზაციის საერთო წესები (IRegistrySyncAdapter.Normalize): ერთი და იგივე შიგთავსი ორივე მხარეს ერთ
//ჰეშს უნდა იძლეოდეს. ყველა წესი იდემპოტენტურია
public static class ContractNormalization
{
    //"" და null ერთნაირად ითვლება: ლოკალური რედაქტორები ხშირად ""-ს ინახავს, სერვერი კი null-ს
    public static string? EmptyToNull(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    //კანონიკური გზის დისკის ასო დიდია: d:\X და D:\X ერთი გზაა. რეალურ მონაცემებში ორივე გვხვდება, Linux-ის
    //ToCanonical კი წესის წერილობას (D:\) აბრუნებს, ამიტომ ერთი ჩანაწერი ორ კომპიუტერზე სხვადასხვა ჰეშს მისცემდა.
    //გზის დანარჩენი ნაწილი არ იცვლება, რადგან Linux-ზე რეგისტრს მნიშვნელობა აქვს
    public static string? UpperDriveLetter(string? path)
    {
        return path is { Length: >= 2 } && path[1] == ':' && char.IsAsciiLetterLower(path[0])
            ? char.ToUpperInvariant(path[0]) + path[1..]
            : path;
    }

    //სიმრავლე სახელით ლაგდება რეგისტრის გარეშე, როგორც სერვერი აბრუნებს. მხოლოდ რეგისტრით განსხვავებული სახელები
    //Ordinal-ით ლაგდება, რომ რიგი ყოველთვის ერთი იყოს
    public static List<T> OrderByName<T>(IEnumerable<T> items, Func<T, string> getName)
    {
        return [.. items.OrderBy(getName, StringComparer.OrdinalIgnoreCase).ThenBy(getName, StringComparer.Ordinal)];
    }

    //enum-ის სახელი კანონიკური წერილობით (მაგ. "sqlserver" → "SqlServer"). უცნობი სახელი უცვლელი რჩება
    public static string EnumName<TEnum>(string name) where TEnum : struct, Enum
    {
        return Enum.TryParse(name, true, out TEnum value) ? value.ToString() : name;
    }

    //სახელი ამ კლიენტის enum-ის მნიშვნელობაა (რეგისტრის გარეშე), ანუ ლოკალურად აისახება
    public static bool IsEnumName<TEnum>(string name) where TEnum : struct, Enum
    {
        return Enum.TryParse(name, true, out TEnum _);
    }
}
