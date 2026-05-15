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
    public PairedDbObjectsModel(List<PairedTable> pairedTables)
    {
        PairedTables = pairedTables;
    }

    public List<PairedTable> PairedTables { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }
}
