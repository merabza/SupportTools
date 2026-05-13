namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

//Dev ბაზის foreign-key დამოკიდებულების ერთი წახნაგი: From-ცხრილს აქვს FK To-ცხრილზე
public sealed class FkEdge
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public FkEdge(string fromSchema, string fromTable, string toSchema, string toTable)
    {
        FromSchema = fromSchema;
        FromTable = fromTable;
        ToSchema = toSchema;
        ToTable = toTable;
    }

    public string FromSchema { get; }
    public string FromTable { get; }
    public string ToSchema { get; }
    public string ToTable { get; }
}
