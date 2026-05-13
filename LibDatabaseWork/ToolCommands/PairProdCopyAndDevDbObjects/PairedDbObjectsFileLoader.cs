using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

//ცხრილების წყვილების JSON ფაილის ჩამტვირთველი და შემნახველი
public static class PairedDbObjectsFileLoader
{
    public static PairedDbObjectsResult Load(string filePath, ILogger logger)
    {
        if (!File.Exists(filePath))
        {
            return new PairedDbObjectsResult();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PairedDbObjectsResult();
            }

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore
            };
            PairedDbObjectsResult? result = JsonConvert.DeserializeObject<PairedDbObjectsResult>(json, settings);
            return result ?? new PairedDbObjectsResult();
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to load paired DB objects file {filePath}", true, logger);
            return new PairedDbObjectsResult();
        }
    }

    public static bool Save(string filePath, PairedDbObjectsResult result, ILogger logger)
    {
        try
        {
            string json = JsonConvert.SerializeObject(result, Formatting.Indented);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to save paired DB objects file {filePath}", true, logger);
            return false;
        }
    }
}
