namespace LibDatabaseWork.Models;

public sealed class ConstraintDataModel
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ConstraintDataModel(string tableName, string columnName, string defaultConstraintName)
    {
        TableName = tableName;
        ColumnName = columnName;
        DefaultConstraintName = defaultConstraintName;
    }

    public string TableName { get; }
    public string ColumnName { get; }
    public string DefaultConstraintName { get; }
}