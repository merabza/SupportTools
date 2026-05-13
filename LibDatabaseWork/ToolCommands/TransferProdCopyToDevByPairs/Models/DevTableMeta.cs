using System;
using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

public sealed class DevTableMeta
{
    public DevTableMeta()
    {
        IdentityColumns = new HashSet<string>(StringComparer.Ordinal);
        ComputedColumns = new HashSet<string>(StringComparer.Ordinal);
    }

    public HashSet<string> IdentityColumns { get; }
    public HashSet<string> ComputedColumns { get; }
}
