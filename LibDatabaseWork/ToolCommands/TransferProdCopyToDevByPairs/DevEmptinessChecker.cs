using System;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//ცარიელობის შემოწმება: Dev-ში pairs-ის თითო ცხრილზე SELECT COUNT_BIG(*) > 0 ნიშნავს ცარიელი არ არის
internal static class DevEmptinessChecker
{
    public static bool AllEmpty(string devConnectionString, PairedDbObjectsResult pairs, ILogger logger,
        out string? firstNonEmptyTable)
    {
        firstNonEmptyTable = null;
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using DbManager? dbm = DbManager.Create(dbKit, devConnectionString);
        if (dbm is null)
        {
            StShared.WriteErrorLine("Cannot create DbManager for Dev database", true, logger);
            return false;
        }

        try
        {
            dbm.Open();
            foreach (PairedTable pt in pairs.PairedTables)
            {
                string fullName = $"[{pt.DevSchemaName}].[{pt.DevTableName}]";
                string query = $"SELECT COUNT_BIG(*) FROM {fullName}";
                long count = dbm.ExecuteScalar<long>(query);
                if (count <= 0)
                {
                    continue;
                }

                firstNonEmptyTable = $"{pt.DevSchemaName}.{pt.DevTableName}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, "Failed to check Dev tables emptiness", true, logger);
            return false;
        }
        finally
        {
            dbm.Close();
        }
    }
}
