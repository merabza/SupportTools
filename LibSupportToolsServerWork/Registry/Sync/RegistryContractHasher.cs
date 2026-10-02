using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LibSupportToolsServerWork.Registry.Sync;

//კონტრაქტის ჰეში: SHA-256 დეტერმინისტული JSON-იდან (README §4.3). ჰეშები ბოლო სინქრონიზაციის მდგომარეობაში ინახება,
//ამიტომ სერიალიზაციის წესები მხოლოდ აქ არის. მათი შეცვლა ყველა შენახულ ჰეშს „ლოკალურად შეცვლილად“ აქცევს; ტესტი
//კანონიკურ JSON-ს ამოწმებს.
//- ზედა დონის Version (optimistic concurrency) ჰეშში არ შედის; ჩადგმული ობიექტების Version ჩვეულებრივი ველია.
//- null-ები და ნაგულისხმევი მნიშვნელობები (0, false) გამოტოვებულია: კონტრაქტში ახალი, ჯერ შეუვსებელი ველის დამატება
//  ჰეშს არ ცვლის. "" და ცარიელი სია კი ჩაიწერება, ამიტომ მათ null-თან გათანაბრება ნორმალიზაციის საქმეა.
//- ობიექტის ველები და dictionary-ის გასაღებები Ordinal-ით ლაგდება. სიების რიგი რჩება: სიმრავლეების დალაგება ადაპტერის
//  ნორმალიზაციის საქმეა (IRegistrySyncAdapter.Normalize)
public static class RegistryContractHasher
{
    private const string VersionPropertyName = "Version";

    private static readonly JsonSerializerSettings HashSerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    public static string ComputeHash(object contract)
    {
        string json = ToCanonicalJson(contract);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    //JSON, რომლის ჰეშიც ითვლება
    internal static string ToCanonicalJson(object contract)
    {
        JToken token = JToken.FromObject(contract, JsonSerializer.Create(HashSerializerSettings));
        if (token is JObject jObject)
        {
            jObject.Remove(VersionPropertyName);
        }

        return Canonicalize(token).ToString(Formatting.None);
    }

    private static JToken Canonicalize(JToken token)
    {
        return token switch
        {
            JObject jObject => new JObject(jObject.Properties().OrderBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => new JProperty(x.Name, Canonicalize(x.Value)))),
            JArray jArray => new JArray(jArray.Select(Canonicalize)),
            _ => token
        };
    }
}
