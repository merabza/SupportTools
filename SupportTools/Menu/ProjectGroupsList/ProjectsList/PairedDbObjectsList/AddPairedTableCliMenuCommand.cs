using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class AddPairedTableCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AddPairedTableCliMenuCommand(ILogger logger, IParametersManager parametersManager, string projectName) :
        base("Add new table pair", EMenuAction.Reload, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Project {_projectName} not found", true);
            return ValueTask.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName))
        {
            StShared.WriteErrorLine($"Project {_projectName} does not contain PairedDbObjectsResultFileName", true);
            return ValueTask.FromResult(false);
        }

        PairedDbObjectsConnectionResolver? resolver =
            PairedDbObjectsConnectionResolver.Create(parameters, project, _logger);
        if (resolver is null)
        {
            return ValueTask.FromResult(false);
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? prodCopyTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(resolver.ProdCopyConnectionString, "ProdCopy", _logger);
        if (prodCopyTables is null)
        {
            return ValueTask.FromResult(false);
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? devTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(resolver.DevConnectionString, "Dev", _logger);
        if (devTables is null)
        {
            return ValueTask.FromResult(false);
        }

        TableInfo? prodTableInfo = PromptTable(prodCopyTables, "ProdCopy");
        if (prodTableInfo is null)
        {
            return ValueTask.FromResult(false);
        }

        TableInfo? devTableInfo = PromptTable(devTables, "Dev");
        if (devTableInfo is null)
        {
            return ValueTask.FromResult(false);
        }

        PairedDbObjectsResult result = PairedDbObjectsFileLoader.Load(project.PairedDbObjectsResultFileName, _logger);

        string newKey = $"{prodTableInfo.SchemaName}.{prodTableInfo.TableName}";
        if (result.PairedTables.Any(pt => PairedTableKeyBuilder.BuildKey(pt) == newKey))
        {
            StShared.WriteErrorLine($"Table pair with ProdCopy table {newKey} already exists", true);
            return ValueTask.FromResult(false);
        }

        List<PairedField> pairedFields = [];
        if (Inputer.InputBool("Auto-pair matching fields by name?", true, false))
        {
            Dictionary<string, string> devColumnLookup =
                devTableInfo.Columns.ToDictionary(c => c.ToLowerInvariant(), c => c);
            foreach (string prodColumn in prodTableInfo.Columns)
            {
                if (devColumnLookup.TryGetValue(prodColumn.ToLowerInvariant(), out string? devColumn))
                {
                    pairedFields.Add(new PairedField(prodColumn, devColumn));
                }
            }
        }

        result.PairedTables.Add(new PairedTable(prodTableInfo.SchemaName, prodTableInfo.TableName,
            devTableInfo.SchemaName, devTableInfo.TableName, pairedFields));

        bool saved = PairedDbObjectsFileLoader.Save(project.PairedDbObjectsResultFileName, result, _logger);
        return ValueTask.FromResult(saved);
    }

    private static TableInfo? PromptTable(Dictionary<(string SchemaLower, string TableLower), TableInfo> tables,
        string sideName)
    {
        if (tables.Count == 0)
        {
            StShared.WriteErrorLine($"No tables found in {sideName} database", true);
            return null;
        }

        List<TableInfo> sortedTables = [.. tables.Values.OrderBy(t => t.SchemaName).ThenBy(t => t.TableName)];
        List<string> tableLabels = sortedTables.Select(t => $"{t.SchemaName}.{t.TableName}").ToList();

        var selectInput = new SelectFromListInput($"{sideName} table", tableLabels);
        if (!selectInput.DoInput() || string.IsNullOrEmpty(selectInput.Text))
        {
            return null;
        }

        return sortedTables.FirstOrDefault(t => $"{t.SchemaName}.{t.TableName}" == selectInput.Text);
    }
}
