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

public sealed class EditPairedFieldCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditPairedFieldCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base("Edit field pair", EMenuAction.Reload)
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
            string.IsNullOrWhiteSpace(_menuParameters.PairedTableKey) ||
            string.IsNullOrWhiteSpace(_menuParameters.PairedFieldKey))
        {
            StShared.WriteErrorLine("Project, pairs file, current table pair, or field pair not set", true);
            return false;
        }

        PairedDbObjectsModel result =
            PairedDbObjectsParametersManager.Load(project.PairedDbObjectsResultFileName, _logger);
        PairedTable? currentTable =
            result.PairedTables.FirstOrDefault(pt =>
                PairedTableKeyBuilder.BuildKey(pt) == _menuParameters.PairedTableKey);
        if (currentTable is null)
        {
            StShared.WriteErrorLine($"Table pair {_menuParameters.PairedTableKey} not found", true);
            return false;
        }

        PairedField? currentField = currentTable.PairedFields.FirstOrDefault(pf =>
            PairedTableKeyBuilder.BuildFieldKey(pf) == _menuParameters.PairedFieldKey);
        if (currentField is null)
        {
            StShared.WriteErrorLine($"Field pair {_menuParameters.PairedFieldKey} not found", true);
            return false;
        }

        var resolver = PairedDbObjectsConnectionResolver.Create(parameters, project, _logger);
        if (resolver is null)
        {
            return false;
        }

        List<string>? prodColumns = ReadColumns(resolver.ProdCopyConnectionString, "ProdCopy",
            currentTable.ProdCopySchemaName, currentTable.ProdCopyTableName);
        if (prodColumns is null)
        {
            return false;
        }

        List<string>? devColumns = ReadColumns(resolver.DevConnectionString, "Dev", currentTable.DevSchemaName,
            currentTable.DevTableName);
        if (devColumns is null)
        {
            return false;
        }

        string? newProdColumn = PromptColumn(prodColumns, "ProdCopy", currentField.ProdCopyFieldName);
        if (string.IsNullOrEmpty(newProdColumn))
        {
            return false;
        }

        if (newProdColumn != currentField.ProdCopyFieldName &&
            currentTable.PairedFields.Any(pf => pf.ProdCopyFieldName == newProdColumn))
        {
            StShared.WriteErrorLine($"Another field pair with ProdCopy field {newProdColumn} already exists", true);
            return false;
        }

        string? newDevColumn = PromptColumn(devColumns, "Dev", currentField.DevFieldName);
        if (string.IsNullOrEmpty(newDevColumn))
        {
            return false;
        }

        currentField.ProdCopyFieldName = newProdColumn;
        currentField.DevFieldName = newDevColumn;

        var parMan = new PairedDbObjectsParametersManager(project.PairedDbObjectsResultFileName, result);
        bool saved = await parMan.Save(result, null, null, cancellationToken);
        if (!saved)
        {
            return false;
        }

        _menuParameters.PairedFieldKey = newProdColumn;
        return true;
    }

    private List<string>? ReadColumns(string connectionString, string sideName, string schemaName, string tableName)
    {
        Dictionary<(string SchemaLower, string TableLower), TableInfo>? tables =
            DbSchemaQueryHelper.ReadTablesAndColumns(connectionString, sideName, _logger);
        if (tables is null)
        {
            return null;
        }

        if (!tables.TryGetValue((schemaName.ToLowerInvariant(), tableName.ToLowerInvariant()),
                out TableInfo? tableInfo))
        {
            StShared.WriteErrorLine($"{sideName} table {schemaName}.{tableName} not found in database", true);
            return null;
        }

        return tableInfo.Columns;
    }

    private static string? PromptColumn(List<string> columns, string sideName, string defaultValue)
    {
        if (columns.Count == 0)
        {
            StShared.WriteErrorLine($"No columns found for {sideName} table", true);
            return null;
        }

        var selectInput = new SelectFromListInput($"{sideName} field", [.. columns.OrderBy(c => c)], defaultValue);
        if (!selectInput.DoInput() || string.IsNullOrEmpty(selectInput.Text))
        {
            return null;
        }

        return selectInput.Text;
    }
}
