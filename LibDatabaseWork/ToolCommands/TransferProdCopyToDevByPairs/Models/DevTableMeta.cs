using System;
using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

public sealed class DevTableMeta
{
    public DevTableMeta()
    {
        IdentityColumns = new HashSet<string>(StringComparer.Ordinal);
        ComputedColumns = new HashSet<string>(StringComparer.Ordinal);
        NotNullWithoutDefaultColumns = new Dictionary<string, string>(StringComparer.Ordinal);
        PrimaryKeyColumns = [];
        UniqueIndexes = [];
    }

    public HashSet<string> IdentityColumns { get; }
    public HashSet<string> ComputedColumns { get; }

    //NOT NULL სვეტები default-ის გარეშე (identity, computed და rowversion გარდა): სვეტი → ტიპის სახელი;
    //წყაროში არარსებული ასეთი სვეტი ჩაწერისას აუცილებლად უნდა შეივსოს
    public Dictionary<string, string> NotNullWithoutDefaultColumns { get; }

    //პირველადი გასაღების სვეტების სია — Adjust ალგორითმისთვის გასაღების სანახავად
    public List<string> PrimaryKeyColumns { get; }

    //უნიკალური ინდექსები (PK-ს ჩათვლით) — bulk insert-ამდე კოლიზიების შესამოწმებლად
    public List<UniqueIndexMeta> UniqueIndexes { get; }
}
