using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairedTable
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedTable(string prodCopySchemaName, string prodCopyTableName, string devSchemaName, string devTableName,
        List<PairedField> pairedFields)
    {
        ProdCopySchemaName = prodCopySchemaName;
        ProdCopyTableName = prodCopyTableName;
        DevSchemaName = devSchemaName;
        DevTableName = devTableName;
        PairedFields = pairedFields;
    }

    public string ProdCopySchemaName { get; }
    public string ProdCopyTableName { get; }
    public string DevSchemaName { get; }
    public string DevTableName { get; }
    public List<PairedField> PairedFields { get; }
}
