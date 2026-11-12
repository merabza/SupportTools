using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairedField : ItemData
{
    public PairedField()
    {
        ProdCopyFieldName = string.Empty;
        DevFieldName = string.Empty;
    }

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedField(string prodCopyFieldName, string devFieldName)
    {
        ProdCopyFieldName = prodCopyFieldName;
        DevFieldName = devFieldName;
    }

    public string ProdCopyFieldName { get; set; }
    public string DevFieldName { get; set; }

    //დაკეტილ ველს "Pair ProdCopy and Dev Db Objects" აღარ ცვლის
    public bool Locked { get; set; }

    public override string GetItemKey()
    {
        return $"{ProdCopyFieldName} - {DevFieldName}";
    }
}
