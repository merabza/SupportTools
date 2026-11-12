using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using AppCliTools.LibDataInput;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;

public sealed class PairProdCopyAndDevDbObjectsToolCommand : ToolCommand
{
    private const string ActionName = "Pair ProdCopy and Dev Db Objects";
    private const string ActionDescription = "Pair ProdCopy and Dev Db Objects by name and save to JSON";
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairProdCopyAndDevDbObjectsToolCommand(ILogger logger, PairDbObjectsParameters parameters,
        IParametersManager? parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
    }

    private PairDbObjectsParameters Parameters => (PairDbObjectsParameters)Par;

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

        if (string.IsNullOrWhiteSpace(Parameters.ResultFileName))
        {
            _logger.LogError("ResultFileName not specified");
            return false;
        }

        return true;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //თუ შედეგების ფაილი უკვე არსებობს, ვკითხოთ მომხმარებელს გადაწერის ნებართვაზე
        if (File.Exists(Parameters.ResultFileName) &&
            !Inputer.InputBool($"File {Parameters.ResultFileName} exists, overwrite?", false, false))
        {
            return false;
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? prodCopyTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(Parameters.ProdCopyDataProvider,
                Parameters.ProdCopyConnectionString, "ProdCopy", _logger);
        if (prodCopyTables is null)
        {
            return false;
        }

        Dictionary<(string SchemaLower, string TableLower), TableInfo>? devTables =
            DbSchemaQueryHelper.ReadTablesAndColumns(Parameters.DevDataProvider, Parameters.DevConnectionString, "Dev",
                _logger);
        if (devTables is null)
        {
            return false;
        }

        //Access-ისა და MariaDB-ის ProdCopy-ს სქემები არ აქვს — Dev ცხრილებთან წყვილდება მხოლოდ ცხრილის სახელით
        if (!DbSchemaQueryHelper.UsesSchemas(Parameters.ProdCopyDataProvider))
        {
            devTables = DbSchemaQueryHelper.ReKeyDevTablesByTableNameOnly(devTables, _logger);
        }

        //არსებული ფაილის დაკეტილი ცხრილები და ველები ახალ წყვილებში უცვლელად გადადის
        PairedDbObjectsModel existing = PairedDbObjectsParametersManager.Load(Parameters.ResultFileName, _logger);
        Dictionary<string, PairedTable> pairedTables = PairedDbObjectsPairer.Pair(prodCopyTables, devTables,
            DbSchemaQueryHelper.UsesSchemas(Parameters.ProdCopyDataProvider), existing.PairedTables, _logger);

        var result = new PairedDbObjectsModel(pairedTables);

        try
        {
            //ParametersManager.Save ფაილის წინა ვერსიას ბექაპად ინახავს (<ფაილი>.yyyyMMdd-HHmmss-fff.bak, 10 უახლესი)
            var parMan = new PairedDbObjectsParametersManager(Parameters.ResultFileName, result);
            if (!await parMan.Save(result, null, null, cancellationToken))
            {
                return false;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Paired DB objects saved to {ResultFileName}. Paired tables: {Count}, locked tables: {LockedCount}",
                    Parameters.ResultFileName, pairedTables.Count, pairedTables.Values.Count(x => x.Locked));
            }
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, true, _logger);
            return false;
        }

        return true;
    }
}
