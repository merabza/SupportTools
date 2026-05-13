namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairedField
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
}
