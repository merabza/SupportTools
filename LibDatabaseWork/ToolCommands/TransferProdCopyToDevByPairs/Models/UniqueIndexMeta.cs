using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

//Dev ცხრილის უნიკალური ინდექსის აღწერა — ჩაწერამდე კოლიზიების აღმოსაჩენად
public sealed class UniqueIndexMeta
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UniqueIndexMeta(string indexName, bool hasFilter)
    {
        IndexName = indexName;
        HasFilter = hasFilter;
        Columns = [];
    }

    public string IndexName { get; }

    //ფილტრიანი ინდექსი NULL-იან მწკრივებს, როგორც წესი, არ ფარავს — შემოწმებისას ისინი გამოტოვდება
    public bool HasFilter { get; }

    //საკვანძო სვეტები key_ordinal-ის მიხედვით დალაგებული
    public List<string> Columns { get; }
}
