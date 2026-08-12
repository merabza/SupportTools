using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//SeederRules-ისა და OldDataConvertor-ის პროცესების StandardOutput-იდან JSON მასივის ამოღება
//და მონაცემთა მწკრივების სიად გადაყვანა
internal static class RunnerOutputParser
{
    public static List<Dictionary<string, object?>>? ParseRowsFromOutput(string stdout, string tableName,
        string sourceLabel, ILogger logger)
    {
        //JSON მასივი ერთ ხაზზეა; stdout-ში build-ისა და ლოგის სტრიქონებიც ხვდება ('['-ით იწყება, მაგრამ ']'-ით არ მთავრდება)
        string? json = null;
        string[] lines = stdout.Split('\n');
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i].Trim();
            if (!line.StartsWith('[') || !line.EndsWith(']'))
            {
                continue;
            }

            json = line;
            break;
        }

        if (json is null)
        {
            StShared.WriteErrorLine($"Could not find JSON array in {sourceLabel} output for table '{tableName}'", true,
                logger);
            return null;
        }

        try
        {
            JArray array = JArray.Parse(json);
            var rows = new List<Dictionary<string, object?>>(array.Count);
            foreach (JToken token in array)
            {
                if (token is not JObject obj)
                {
                    continue;
                }

                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (JProperty prop in obj.Properties())
                {
                    row[prop.Name] = JsonValueToClrValue(prop.Value);
                }

                rows.Add(row);
            }

            return rows;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to parse JSON from {sourceLabel} for table '{tableName}'", true,
                logger);
            return null;
        }
    }

    private static object? JsonValueToClrValue(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Null or JTokenType.Undefined => null,
            JTokenType.Integer => token.Value<long>(),
            JTokenType.Float => token.Value<double>(),
            JTokenType.String => token.Value<string>(),
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.Date => token.Value<DateTime>(),
            JTokenType.Bytes => token.Value<byte[]>(),
            JTokenType.Guid => token.Value<Guid>(),
            _ => token.ToString()
        };
    }
}
