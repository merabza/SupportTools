using System.Collections.Generic;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;

public sealed class ValidationReport
{
    public ValidationReport()
    {
        MissingTables = [];
        MissingFields = [];
    }

    public List<string> MissingTables { get; }
    public List<string> MissingFields { get; }

    public bool IsValid => MissingTables.Count == 0 && MissingFields.Count == 0;
}
