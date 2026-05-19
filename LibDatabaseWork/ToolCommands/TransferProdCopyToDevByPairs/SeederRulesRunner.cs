using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneOf;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.Errors;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//SeederRules-ის გამოძახება: dotnet პროექტი იშვება ცხრილის სახელით და დააბრუნებს StandardOutput-ში JSON-ის მასივს
public static class SeederRulesRunner
{
    private const string LogFolder = @"d:\Logs";
    private const string DotnetExecutable = "dotnet";

    public static List<Dictionary<string, object?>>? Run(string projectFilePath, string tableName, ILogger logger)
    {
        if (!File.Exists(projectFilePath))
        {
            StShared.WriteErrorLine($"DataSeederRules project file '{projectFilePath}' does not exist", true, logger);
            return null;
        }

        string arguments =
            $"run --project \"{projectFilePath}\" -- --table-name {tableName} --log-folder \"{LogFolder}\"";
        OneOf<(string, int), Error[]> processResult =
            StShared.RunProcessWithOutput(false, logger, DotnetExecutable, arguments);
        if (processResult.IsT1)
        {
            StShared.WriteErrorLine($"DataSeederRules process failed for table '{tableName}'", true, logger);
            return null;
        }

        string stdout = processResult.AsT0.Item1;
        return ParseRowsFromOutput(stdout, tableName, logger);
    }

    //StandardOutput-ში JSON მასივის ამოღება და მონაცემთა მწკრივების სიად გადაყვანა
    private static List<Dictionary<string, object?>>? ParseRowsFromOutput(string stdout, string tableName,
        ILogger logger)
    {
        int firstBracket = stdout.IndexOf('[');
        int lastBracket = stdout.LastIndexOf(']');
        if (firstBracket < 0 || lastBracket < 0 || lastBracket <= firstBracket)
        {
            StShared.WriteErrorLine($"Could not find JSON array in DataSeederRules output for table '{tableName}'",
                true, logger);
            return null;
        }

        string json = stdout.Substring(firstBracket, lastBracket - firstBracket + 1);
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
            StShared.WriteException(ex, $"Failed to parse JSON from DataSeederRules for table '{tableName}'", true,
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
