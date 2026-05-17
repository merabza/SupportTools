using System;
using System.IO;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public class PairedDbObjectsParametersManager : ParametersManager
{
    public PairedDbObjectsParametersManager(string parametersFileName, IParameters parameters) : base(
        parametersFileName, parameters)
    {
    }

    public static PairedDbObjectsModel Load(string filePath, ILogger logger)
    {
        if (!File.Exists(filePath))
        {
            return new PairedDbObjectsModel();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PairedDbObjectsModel();
            }

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var result = JsonConvert.DeserializeObject<PairedDbObjectsModel>(json, settings);
            return result ?? new PairedDbObjectsModel();
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, $"Failed to load paired DB objects file {filePath}", true, logger);
            return new PairedDbObjectsModel();
        }
    }
}
