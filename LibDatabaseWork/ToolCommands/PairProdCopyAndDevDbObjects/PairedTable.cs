using System.Collections.Generic;
using SystemTools.DatabaseToolsShared;

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
        SeedDataType = ESeedDataType.OnlyDatabase;
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
        SeedDataType = ESeedDataType.OnlyDatabase;
    }

    public string ProdCopySchemaName { get; set; }
    public string ProdCopyTableName { get; set; }
    public string DevSchemaName { get; set; }
    public string DevTableName { get; set; }
    public List<PairedField> PairedFields { get; set; }
    public ESeedDataType SeedDataType { get; set; }
}
