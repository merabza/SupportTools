namespace SupportTools.Menu;

public class SupportToolsMenuParameters
{
    public string ProjectGroupName { get; set; }
    public string ProjectName { get; set; }

    //მიმდინარე ცხრილების წყვილის გასაღები (ProdCopy მხარიდან: "schema.table")
    public string? PairedTableKey { get; set; }

    //მიმდინარე ველების წყვილის გასაღები (ProdCopy მხარიდან: "fieldName")
    public string? PairedFieldKey { get; set; }
}
