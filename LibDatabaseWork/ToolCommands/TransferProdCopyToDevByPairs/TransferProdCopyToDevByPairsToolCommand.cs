using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.LibDataInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//ProdCopy → Dev ბაზაში ინფორმაციის პირდაპირი გადატანა pairs ფაილის მიხედვით, scaffolding-ის გარეშე
public sealed class TransferProdCopyToDevByPairsToolCommand : ToolCommand
{
    private const string ActionName = "Transfer ProdCopy To Dev By Pairs";
    private const string ActionDescription = "Transfer data from ProdCopy to Dev database using paired DB objects";
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TransferProdCopyToDevByPairsToolCommand(ILogger logger, TransferProdCopyToDevByPairsParameters parameters,
        IParametersManager? parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
    }

    private TransferProdCopyToDevByPairsParameters Parameters => (TransferProdCopyToDevByPairsParameters)Par;

    protected override bool CheckValidate()
    {
        if (Parameters.ProdCopyDataProvider == EDatabaseProvider.None)
        {
            _logger.LogError("ProdCopyDataProvider not specified");
            return false;
        }

        if (Parameters.DevDataProvider == EDatabaseProvider.None)
        {
            _logger.LogError("DevDataProvider not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.ProdCopyConnectionString))
        {
            _logger.LogError("ProdCopyConnectionString not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.DevConnectionString))
        {
            _logger.LogError("DevConnectionString not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.PairedDbObjectsResultFileName))
        {
            _logger.LogError("PairedDbObjectsResultFileName not specified");
            return false;
        }

        return true;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //1. ProdCopy schema
        Dictionary<(string Schema, string Table), TableInfo>? prodCopySchema =
            DbSchemaQueryHelper.ReadTablesAndColumnsCaseSensitive(Parameters.ProdCopyConnectionString, "ProdCopy",
                _logger);
        if (prodCopySchema is null)
        {
            return false;
        }

        //2. Dev schema
        Dictionary<(string Schema, string Table), TableInfo>? devSchema =
            DbSchemaQueryHelper.ReadTablesAndColumnsCaseSensitive(Parameters.DevConnectionString, "Dev", _logger);
        if (devSchema is null)
        {
            return false;
        }

        //3. Pairs ფაილი — საჭიროების შემთხვევაში ავტო-დაგენერირება
        if (!File.Exists(Parameters.PairedDbObjectsResultFileName))
        {
            if (!Inputer.InputBool(
                    $"Paired DB objects file {Parameters.PairedDbObjectsResultFileName} not found. Auto-generate now?",
                    true, false))
            {
                StShared.WriteErrorLine("Cannot proceed without paired DB objects file", true, _logger);
                return false;
            }

            if (!await PairsAutoGenerator.GenerateAndSave(Parameters.ProdCopyConnectionString,
                    Parameters.DevConnectionString, Parameters.PairedDbObjectsResultFileName, _logger,
                    cancellationToken))
            {
                return false;
            }
        }

        PairedDbObjectsModel pairs =
            PairedDbObjectsParametersManager.Load(Parameters.PairedDbObjectsResultFileName, _logger);
        if (pairs.PairedTables.Count == 0)
        {
            StShared.WriteErrorLine("Paired DB objects file is empty — nothing to transfer", true, _logger);
            return false;
        }

        //4. Pairs vs ProdCopy schema (case-sensitive)
        ValidationReport prodCopyReport =
            PairsSchemaValidator.Validate(pairs, prodCopySchema, true, "ProdCopy", _logger);
        if (!prodCopyReport.IsValid)
        {
            return false;
        }

        //5. Pairs vs Dev schema (case-sensitive)
        ValidationReport devReport = PairsSchemaValidator.Validate(pairs, devSchema, false, "Dev", _logger);
        if (!devReport.IsValid)
        {
            return false;
        }

        //6. ცარიელობის შემოწმება
        if (!DevEmptinessChecker.AllEmpty(Parameters.DevConnectionString, pairs, _logger,
                out string? firstNonEmptyTable))
        {
            if (firstNonEmptyTable is not null)
            {
                StShared.WriteErrorLine(
                    $"Table {firstNonEmptyTable} in Dev database is not empty. Run 'Recreate Dev Database' first, then try again.",
                    true, _logger);
            }

            return false;
        }

        //7. Dev runtime metadata
        Dictionary<(string Schema, string Table), DevTableMeta>? devMeta =
            DevTableMetaReader.Read(Parameters.DevConnectionString, pairs, _logger);
        if (devMeta is null)
        {
            return false;
        }

        //8. FK graph + topological sort
        List<FkEdge>? fkEdges = DevFkGraphReader.Read(Parameters.DevConnectionString, _logger);
        if (fkEdges is null)
        {
            return false;
        }

        List<(string Schema, string Table)> nodes = pairs.PairedTables.Values
            .Select(p => (p.DevSchemaName, p.DevTableName)).ToList();
        TopologicalSorter.SortResult sortResult = TopologicalSorter.Sort(nodes, fkEdges);
        if (sortResult.HasCycle)
        {
            StShared.WriteErrorLine(
                "FK cycle detected among tables:\n" +
                string.Join("\n", sortResult.Cycle!.Select(c => $"  - {c.Schema}.{c.Table}")) +
                "\nCannot determine safe insert order. Fix circular foreign-key dependencies in the Dev schema before running this tool.",
                true, _logger);
            return false;
        }

        //9. დადასტურება
        if (!Inputer.InputBool($"About to transfer {pairs.PairedTables.Count} tables from ProdCopy to Dev. Continue?",
                true, false))
        {
            return false;
        }

        //10. გადატანა ტოპოლოგიური თანმიმდევრობით
        Dictionary<(string DevSchemaName, string DevTableName), PairedTable> pairsByDevKey =
            pairs.PairedTables.Values.ToDictionary(p => (p.DevSchemaName, p.DevTableName), p => p);
        long totalRows = 0;
        var stopwatch = Stopwatch.StartNew();
        foreach ((string Schema, string Table) node in sortResult.Ordered!)
        {
            PairedTable pt = pairsByDevKey[node];
            DevTableMeta meta = devMeta[node];

            List<PairedField> insertable = pt.PairedFields.Values
                .Where(f => !meta.ComputedColumns.Contains(f.DevFieldName)).ToList();
            bool hasIdentity = insertable.Any(f => meta.IdentityColumns.Contains(f.DevFieldName));

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Transferring {Schema}.{Table} ({FieldCount} fields, identity={HasIdentity})",
                    pt.DevSchemaName, pt.DevTableName, insertable.Count, hasIdentity);
            }

            long rows = await TableDataTransferrer.TransferAsync(Parameters.ProdCopyConnectionString,
                Parameters.DevConnectionString, pt, insertable, hasIdentity, Parameters.CommandTimeOut, _logger,
                cancellationToken);
            totalRows += rows;

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Copied {Rows} rows to {Schema}.{Table}", rows, pt.DevSchemaName,
                    pt.DevTableName);
            }
        }

        stopwatch.Stop();
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Transfer complete: {TotalRows} rows across {TableCount} tables in {Elapsed}",
                totalRows, pairs.PairedTables.Count, stopwatch.Elapsed);
        }

        return true;
    }
}
