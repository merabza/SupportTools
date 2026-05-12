using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;

public sealed class TableInfo
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public TableInfo(string schemaName, string tableName)
    {
        SchemaName = schemaName;
        TableName = tableName;
    }

    public string SchemaName { get; }
    public string TableName { get; }
    public List<string> Columns { get; } = [];
}
