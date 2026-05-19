using System;
using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

public sealed class DevTableMeta
{
    public DevTableMeta()
    {
        IdentityColumns = new HashSet<string>(StringComparer.Ordinal);
        ComputedColumns = new HashSet<string>(StringComparer.Ordinal);
        PrimaryKeyColumns = [];
    }

    public HashSet<string> IdentityColumns { get; }
    public HashSet<string> ComputedColumns { get; }

    //პირველადი გასაღების სვეტების სია — Adjust ალგორითმისთვის გასაღების სანახავად
    public List<string> PrimaryKeyColumns { get; }
}
