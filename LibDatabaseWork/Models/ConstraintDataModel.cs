namespace LibDatabaseWork.Models;

public sealed class ConstraintDataModel
{
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