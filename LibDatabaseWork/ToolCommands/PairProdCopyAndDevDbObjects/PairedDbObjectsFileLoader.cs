//using System;
//using System.IO;
//using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using SystemTools.SystemToolsShared;

//namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

////ცხრილების წყვილების JSON ფაილის ჩამტვირთველი და შემნახველი
//public static class PairedDbObjectsFileLoader
//{
//    public static PairedDbObjectsModel Load(string filePath, ILogger logger)
//    {
//        if (!File.Exists(filePath))
//        {
//            return new PairedDbObjectsModel();
//        }

//        try
//        {
//            string json = File.ReadAllText(filePath);
//            if (string.IsNullOrWhiteSpace(json))
//            {
//                return new PairedDbObjectsModel();
//            }

//            var settings = new JsonSerializerSettings
//            {
//                NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore
//            };
//            PairedDbObjectsModel? result = JsonConvert.DeserializeObject<PairedDbObjectsModel>(json, settings);
//            return result ?? new PairedDbObjectsModel();
//        }
//        catch (Exception ex)
//        {
//            StShared.WriteException(ex, $"Failed to load paired DB objects file {filePath}", true, logger);
//            return new PairedDbObjectsModel();
//        }
//    }

//    public static bool Save(string filePath, PairedDbObjectsModel result, ILogger logger)
//    {
//        try
//        {
//            string json = JsonConvert.SerializeObject(result, Formatting.Indented);
//            File.WriteAllText(filePath, json);
//            return true;
//        }
//        catch (Exception ex)
//        {
//            StShared.WriteException(ex, $"Failed to save paired DB objects file {filePath}", true, logger);
//            return false;
//        }
//    }
//}
