namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairedField
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedField(string prodCopyFieldName, string devFieldName)
    {
        ProdCopyFieldName = prodCopyFieldName;
        DevFieldName = devFieldName;
    }

    public string ProdCopyFieldName { get; }
    public string DevFieldName { get; }
}
