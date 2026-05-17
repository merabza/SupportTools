using System.Collections.Generic;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;

namespace SupportTools.Cruders;

public sealed class PairedTableCruder : ParCruder<PairedTable>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedTableCruder(IParametersManager parametersManager,
        Dictionary<string, PairedTable> currentValuesDictionary, ILogger logger, string prodCopyConnectionString,
        string devConnectionString) : base(parametersManager, currentValuesDictionary, "Paired Table", "Paired Tables",
        true)
    {
        FieldEditors.Add(new SchemaNameFieldEditor(nameof(PairedTable.ProdCopySchemaName), logger, "ProdCopy",
            prodCopyConnectionString));
        FieldEditors.Add(new TableNameFieldEditor(nameof(PairedTable.ProdCopyTableName), logger, "ProdCopy",
            prodCopyConnectionString));
        FieldEditors.Add(new SchemaNameFieldEditor(nameof(PairedTable.DevSchemaName), logger, "Dev",
            devConnectionString));
        FieldEditors.Add(new TableNameFieldEditor(nameof(PairedTable.DevTableName), logger, "Dev",
            devConnectionString));
        FieldEditors.Add(new EnumFieldEditor<ESeedDataType>(nameof(PairedTable.SeedDataType),
            ESeedDataType.OnlyDatabase));
        FieldEditors.Add(new PairedFieldsListFieldEditor(nameof(PairedTable.PairedFields), logger, parametersManager,
            prodCopyConnectionString, devConnectionString));
    }

    public static PairedTableCruder Create(IParametersManager parametersManager, ILogger logger,
        Dictionary<string, PairedTable> tables, string prodCopyConnectionString, string devConnectionString)
    {
        return new PairedTableCruder(parametersManager, tables, logger, prodCopyConnectionString, devConnectionString);
    }
}
