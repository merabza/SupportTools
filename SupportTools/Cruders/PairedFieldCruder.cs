using System.Collections.Generic;
using AppCliTools.CliParameters;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.FieldEditors;

namespace SupportTools.Cruders;

public sealed class PairedFieldCruder : ParCruder<PairedField>
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedFieldCruder(IParametersManager parametersManager,
        Dictionary<string, PairedField> currentValuesDictionary, ILogger logger, string prodCopyConnectionString,
        string devConnectionString, string prodCopySchemaName, string prodCopyTableName, string devSchemaName,
        string devTableName) : base(parametersManager, currentValuesDictionary, "Paired Field", "Paired Fields", true)
    {
        FieldEditors.Add(new FieldNameFieldEditor(nameof(PairedField.ProdCopyFieldName), logger, "ProdCopy",
            prodCopyConnectionString, prodCopySchemaName, prodCopyTableName));
        FieldEditors.Add(new FieldNameFieldEditor(nameof(PairedField.DevFieldName), logger, "Dev", devConnectionString,
            devSchemaName, devTableName));
    }
}
