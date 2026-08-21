using System.Collections.Generic;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.FieldEditors;

//ცხრილების წყვილში შემავალი ველების წყვილების სიის რედაქტორი (გადადის PairedFieldCruder-ის სიის მენიუზე)
public sealed class PairedFieldsListFieldEditor : FieldEditor<Dictionary<string, PairedField>>
{
    private readonly string _devConnectionString;
    private readonly EDatabaseProvider _devDataProvider;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _prodCopyConnectionString;

    private readonly EDatabaseProvider _prodCopyDataProvider;
    //private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedFieldsListFieldEditor(string propertyName, ILogger logger, IParametersManager parametersManager,
        EDatabaseProvider prodCopyDataProvider, string prodCopyConnectionString, EDatabaseProvider devDataProvider,
        string devConnectionString) : base(propertyName, false, null, false, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _prodCopyDataProvider = prodCopyDataProvider;
        _prodCopyConnectionString = prodCopyConnectionString;
        _devDataProvider = devDataProvider;
        _devConnectionString = devConnectionString;
        //_projectName = projectName;
    }

    public override CliMenuSet GetSubMenu(object record)
    {
        var pairedTable = (PairedTable)record;
        Dictionary<string, PairedField> currentValuesDict = pairedTable.PairedFields;

        var cruder = new PairedFieldCruder(_parametersManager, currentValuesDict, _logger, _prodCopyDataProvider,
            _prodCopyConnectionString, _devDataProvider, _devConnectionString, pairedTable.ProdCopySchemaName,
            pairedTable.ProdCopyTableName, pairedTable.DevSchemaName, pairedTable.DevTableName);
        return cruder.GetListMenu();
    }

    public override string GetValueStatus(object? record)
    {
        Dictionary<string, PairedField>? val = GetValueOrDefault(record);
        if (val is null || val.Count == 0)
        {
            return "No field pairs";
        }

        return val.Count == 1 ? "1 field pair" : $"{val.Count} field pairs";
    }
}
