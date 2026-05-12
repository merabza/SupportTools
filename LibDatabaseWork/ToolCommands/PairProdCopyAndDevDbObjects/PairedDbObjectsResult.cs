using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairedDbObjectsResult
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedDbObjectsResult(List<PairedTable> pairedTables)
    {
        PairedTables = pairedTables;
    }

    public List<PairedTable> PairedTables { get; }
}
