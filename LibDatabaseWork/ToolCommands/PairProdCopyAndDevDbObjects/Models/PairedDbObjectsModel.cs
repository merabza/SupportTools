using System.Collections.Generic;
using ParametersManagement.LibParameters;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;

public sealed class PairedDbObjectsModel : IParameters
{
    public PairedDbObjectsModel()
    {
        PairedTables = [];
    }

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedDbObjectsModel(Dictionary<string, PairedTable> pairedTables)
    {
        PairedTables = pairedTables;
    }

    public Dictionary<string, PairedTable> PairedTables { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }
}
