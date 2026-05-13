using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

//ცხრილების და ველების წყვილების სინთეზური გასაღებების მშენებელი
internal static class PairedTableKeyBuilder
{
    public static string BuildKey(PairedTable pairedTable)
    {
        return $"{pairedTable.ProdCopySchemaName}.{pairedTable.ProdCopyTableName}";
    }

    public static string BuildFieldKey(PairedField pairedField)
    {
        return pairedField.ProdCopyFieldName;
    }
}
