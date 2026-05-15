using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibMenuInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class EditPairedTableCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditPairedTableCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base("Edit table pair", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_menuParameters.ProjectName);
        if (project is null || string.IsNullOrWhiteSpace(project.PairedDbObjectsResultFileName) ||
            string.IsNullOrWhiteSpace(_menuParameters.PairedTableKey))
        {
            StShared.WriteErrorLine("Project, pairs file, or current table pair not set", true);
            return false;
        }

        PairedDbObjectsModel result =
            PairedDbObjectsParametersManager.Load(project.PairedDbObjectsResultFileName, _logger);
        PairedTable? current =
            result.PairedTables.FirstOrDefault(pt =>
                PairedTableKeyBuilder.BuildKey(pt) == _menuParameters.PairedTableKey);
        if (current is null)
        {
            StShared.WriteErrorLine($"Table pair {_menuParameters.PairedTableKey} not found in file", true);
            return false;
        }

        var resolver = PairedDbObjectsConnectionResolver.Create(parameters, project, _logger);
        if (resolver is null)
        {
            return false;
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? prodCopyTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(resolver.ProdCopyConnectionString, "ProdCopy", _logger);
        if (prodCopyTables is null)
        {
            return false;
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? devTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(resolver.DevConnectionString, "Dev", _logger);
        if (devTables is null)
        {
            return false;
        }

        TableInfo? newProd = PromptTable(prodCopyTables, "ProdCopy",
            $"{current.ProdCopySchemaName}.{current.ProdCopyTableName}");
        if (newProd is null)
        {
            return false;
        }

        TableInfo? newDev = PromptTable(devTables, "Dev", $"{current.DevSchemaName}.{current.DevTableName}");
        if (newDev is null)
        {
            return false;
        }

        string newKey = $"{newProd.SchemaName}.{newProd.TableName}";
        if (newKey != _menuParameters.PairedTableKey &&
            result.PairedTables.Any(pt => PairedTableKeyBuilder.BuildKey(pt) == newKey))
        {
            StShared.WriteErrorLine($"Another table pair with ProdCopy table {newKey} already exists", true);
            return false;
        }

        current.ProdCopySchemaName = newProd.SchemaName;
        current.ProdCopyTableName = newProd.TableName;
        current.DevSchemaName = newDev.SchemaName;
        current.DevTableName = newDev.TableName;

        var parMan = new PairedDbObjectsParametersManager(project.PairedDbObjectsResultFileName, result);
        bool saved = await parMan.Save(result, null, null, cancellationToken);
        if (!saved)
        {
            return false;
        }

        //განვაახლოთ მიმდინარე გასაღები
        _menuParameters.PairedTableKey = newKey;
        return true;
    }

    private static TableInfo? PromptTable(Dictionary<(string SchemaLower, string TableLower), TableInfo> tables,
        string sideName, string defaultLabel)
    {
        if (tables.Count == 0)
        {
            StShared.WriteErrorLine($"No tables found in {sideName} database", true);
            return null;
        }

        List<TableInfo> sortedTables = [.. tables.Values.OrderBy(t => t.SchemaName).ThenBy(t => t.TableName)];
        List<string> tableLabels = sortedTables.Select(t => $"{t.SchemaName}.{t.TableName}").ToList();

        var selectInput = new SelectFromListInput($"{sideName} table", tableLabels, defaultLabel);
        if (!selectInput.DoInput() || string.IsNullOrEmpty(selectInput.Text))
        {
            return null;
        }

        return sortedTables.FirstOrDefault(t => $"{t.SchemaName}.{t.TableName}" == selectInput.Text);
    }
}
