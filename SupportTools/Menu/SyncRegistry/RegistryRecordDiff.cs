using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SupportToolsServerApiContracts.Models;

namespace SupportTools.Menu.SyncRegistry;

//ჩანაწერის ორი მხარის (ლოკალური და სერვერის კონტრაქტის) შედარება ველების დონეზე, გამოსატანად (C5). საიდუმლო ველის
//მნიშვნელობა არასოდეს ჩანს (G2, README §7 წესი 8): შედარება ნამდვილი მნიშვნელობით ხდება, შედეგში კი HiddenValue
//წერია. ველები ჰეშის წესით იკითხება (RegistryContractHasher): ზედა დონის Version (optimistic concurrency) არ
//შედარდება, null-ები და ნაგულისხმევი მნიშვნელობები გამოტოვებულია, ამიტომ განსხვავება ჰეშის განსხვავებას შეესაბამება
internal static class RegistryRecordDiff
{
    public const string HiddenValue = "***";

    private const string VersionPropertyName = "Version";

    //ველის გზაში ჩადგმული ველის და სიის ელემენტის გამყოფები: ServerInfos[0].ServerName
    private static readonly char[] PathSeparators = ['.', '['];

    //საიდუმლო ველები ნებისმიერ დონეზე: პაროლები და მომხმარებლები, API key, KeyGuidPart, MediatR-ის ლიცენზია და
    //ფაილების შიგთავსი (.gitignore და .editorconfig შაბლონები, საიდუმლო ფაილები). ფაილსაცავის UserName ბაზის
    //ServerUser-ის მსგავსი ანგარიშის სახელია. სერვერის FilesUserName და ServiceUserName OS-ის ანგარიშებია და ჩანს
    private static readonly HashSet<string> SecretFieldNames = new(StringComparer.Ordinal)
    {
        nameof(StsFileStorageDataModel.Password),
        nameof(StsFileStorageDataModel.UserName),
        nameof(StsDatabaseServerConnectionDataModel.ServerPass),
        nameof(StsDatabaseServerConnectionDataModel.ServerUser),
        nameof(StsApiClientDataModel.ApiKey),
        nameof(StsProjectDataModel.KeyGuidPart),
        nameof(StsGlobalSettingsDataModel.MediatRLicenseKey),
        nameof(StsGitIgnoreFileTypeDataModel.Content)
    };

    private static readonly JsonSerializer FieldsSerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore
    });

    //განსხვავებული ველები ველების რიგით: ჯერ ლოკალური მხარისა, მერე მხოლოდ სერვერზე არსებულები. null მხარე
    //(მაგ. ჩანაწერი სერვერზე წაიშალა) ველებს არ შეიცავს
    public static List<RegistryFieldDifference> Compare(object? local, object? server)
    {
        List<Field> localFields = GetFields(local);
        List<Field> serverFields = GetFields(server);
        Dictionary<string, Field> localByPath = localFields.ToDictionary(x => x.Path, StringComparer.Ordinal);
        Dictionary<string, Field> serverByPath = serverFields.ToDictionary(x => x.Path, StringComparer.Ordinal);

        List<RegistryFieldDifference> differences = [];
        foreach (string path in localFields.Select(x => x.Path).Union(serverFields.Select(x => x.Path),
                     StringComparer.Ordinal))
        {
            Field? localField = localByPath.GetValueOrDefault(path);
            Field? serverField = serverByPath.GetValueOrDefault(path);
            if (!string.Equals(localField?.Value, serverField?.Value, StringComparison.Ordinal))
            {
                differences.Add(new RegistryFieldDifference(path, localField?.Display, serverField?.Display));
            }
        }

        return differences;
    }

    //ზედა დონის ველები, რომლებშიც მხარეები განსხვავდება, მნიშვნელობების გარეშე (მაგ. ServerInfos, SolutionFileName)
    public static List<string> GetChangedFieldNames(object? local, object? server)
    {
        return [.. Compare(local, server).Select(x => GetTopLevelName(x.Path)).Distinct(StringComparer.Ordinal)];
    }

    private static string GetTopLevelName(string path)
    {
        return path.Split(PathSeparators)[0];
    }

    //ველები მოძებნის რიგით: ობიექტის ველები Ordinal-ით დალაგებული, სიის ელემენტები ინდექსით
    private static List<Field> GetFields(object? contract)
    {
        List<Field> fields = [];
        if (contract is null)
        {
            return fields;
        }

        JToken token = JToken.FromObject(contract, FieldsSerializer);
        if (token is JObject root)
        {
            root.Remove(VersionPropertyName);
        }

        AddFields(fields, token, string.Empty, false);
        return fields;
    }

    private static void AddFields(List<Field> fields, JToken token, string path, bool isSecret)
    {
        switch (token)
        {
            case JObject { Count: > 0 } jObject:
                foreach (JProperty property in jObject.Properties().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    AddFields(fields, property.Value, path.Length == 0 ? property.Name : $"{path}.{property.Name}",
                        isSecret || SecretFieldNames.Contains(property.Name));
                }

                break;
            case JArray { Count: > 0 } jArray:
                for (int i = 0; i < jArray.Count; i++)
                {
                    AddFields(fields, jArray[i], $"{path}[{i}]", isSecret);
                }

                break;
            default:
                //ტექსტი ბრჭყალების გარეშე, დანარჩენი JSON-ის ფორმით (რიცხვი, true, [], {})
                string value = token is JValue { Value: string text } ? text : token.ToString(Formatting.None);
                fields.Add(new Field(path, value, isSecret ? HiddenValue : value));
                break;
        }
    }

    //Value შედარებისთვისაა და გარეთ არ გადის; Display გამოსატანია
    private sealed record Field(string Path, string Value, string Display);
}
