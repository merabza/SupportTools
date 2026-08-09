namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

//SeederRules-ის JSON-ის მიმართვითი (გასაღებიანი) ველის შესაბამისობა Dev ცხრილის FK სვეტთან და საძიებო ცნობართან
public sealed class SeederReferenceRule
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SeederReferenceRule(string ruleFieldName, string targetColumnName, string lookupTableName,
        string lookupKeyColumnName, string lookupIdColumnName)
    {
        RuleFieldName = ruleFieldName;
        TargetColumnName = targetColumnName;
        LookupTableName = lookupTableName;
        LookupKeyColumnName = lookupKeyColumnName;
        LookupIdColumnName = lookupIdColumnName;
    }

    //ველი rules-ის JSON-ში (მაგ. PtIdDtKey)
    public string RuleFieldName { get; }

    //Dev ცხრილის FK სვეტი (მაგ. PtId)
    public string TargetColumnName { get; }

    //ცნობარი ცხრილი Dev ბაზაში (მაგ. DataTypes)
    public string LookupTableName { get; }

    //ცნობარის ბუნებრივი გასაღების სვეტი (მაგ. DtTable)
    public string LookupKeyColumnName { get; }

    //ცნობარის identity ID სვეტი (მაგ. DtId)
    public string LookupIdColumnName { get; }
}
