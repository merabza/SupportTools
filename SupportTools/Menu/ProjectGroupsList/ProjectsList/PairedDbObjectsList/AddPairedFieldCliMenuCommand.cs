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

public sealed class AddPairedFieldCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AddPairedFieldCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsMenuParameters menuParameters) : base("Add new field pair", EMenuAction.Reload)
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
        PairedTable? currentTable =
            result.PairedTables.FirstOrDefault(pt =>
                PairedTableKeyBuilder.BuildKey(pt) == _menuParameters.PairedTableKey);
        if (currentTable is null)
        {
            StShared.WriteErrorLine($"Table pair {_menuParameters.PairedTableKey} not found", true);
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

        string? prodColumn = PromptColumn(prodColumns, "ProdCopy");
        if (string.IsNullOrEmpty(prodColumn))
        {
            return false;
        }

        if (currentTable.PairedFields.Any(pf => pf.ProdCopyFieldName == prodColumn))
        {
            StShared.WriteErrorLine($"Field pair with ProdCopy field {prodColumn} already exists", true);
            return false;
        }

        string? devColumn = PromptColumn(devColumns, "Dev");
        if (string.IsNullOrEmpty(devColumn))
        {
            return false;
        }

        currentTable.PairedFields.Add(new PairedField(prodColumn, devColumn));

        var parMan = new PairedDbObjectsParametersManager(project.PairedDbObjectsResultFileName, result);
        bool saved = await parMan.Save(result, null, null, cancellationToken);
        return saved;
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

    private static string? PromptColumn(List<string> columns, string sideName)
    {
        if (columns.Count == 0)
        {
            StShared.WriteErrorLine($"No columns found for {sideName} table", true);
            return null;
        }

        var selectInput = new SelectFromListInput($"{sideName} field", [.. columns.OrderBy(c => c)]);
        if (!selectInput.DoInput() || string.IsNullOrEmpty(selectInput.Text))
        {
            return null;
        }

        return selectInput.Text;
    }
}
