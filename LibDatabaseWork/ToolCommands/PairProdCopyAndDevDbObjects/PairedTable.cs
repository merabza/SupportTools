using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairedTable
{
    public PairedTable()
    {
        ProdCopySchemaName = string.Empty;
        ProdCopyTableName = string.Empty;
        DevSchemaName = string.Empty;
        DevTableName = string.Empty;
        PairedFields = [];
    }

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

    public string ProdCopySchemaName { get; set; }
    public string ProdCopyTableName { get; set; }
    public string DevSchemaName { get; set; }
    public string DevTableName { get; set; }
    public List<PairedField> PairedFields { get; set; }
}
