using System;
using System.Collections.Generic;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//SqlBulkCopy მონაცემებს კავშირების (FK) შემოწმების გარეშე წერს და SQL Server ჩატვირთული ცხრილების კავშირებს
//untrusted-ად ნიშნავს (SSMS-ში "Check Existing Data On Creation Or Re-Enabling" = No).
//ამიტომ გადატანის ბოლოს თითოეული ცხრილის ყველა კავშირი არსებული მონაცემების შემოწმებით თავიდან ირთვება —
//WITH CHECK CHECK CONSTRAINT ALL გადაამოწმებს ჩატვირთულ მონაცემებს და კავშირებს ისევ სანდოს (trusted) გახდის
internal static class DevFkRevalidator
{
    public static bool Revalidate(string devConnectionString, IReadOnlyList<(string Schema, string Table)> tables,
        int commandTimeOut, ILogger logger)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using var dbm = DbManager.Create(dbKit, devConnectionString, commandTimeOut);
        if (dbm is null)
        {
            StShared.WriteErrorLine("Cannot create DbManager for Dev database", true, logger);
            return false;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Re-validating foreign keys on {TableCount} tables", tables.Count);
        }

        bool success = true;
        try
        {
            dbm.Open();
            foreach ((string Schema, string Table) node in tables)
            {
                try
                {
                    dbm.ExecuteNonQuery($"ALTER TABLE [{node.Schema}].[{node.Table}] WITH CHECK CHECK CONSTRAINT ALL");
                }
                catch (Exception ex)
                {
                    //შეცდომისას გადამოწმება გრძელდება, რომ ერთ გაშვებაზე ყველა პრობლემური ცხრილი გამოჩნდეს
                    StShared.WriteException(ex, $"Foreign key re-validation failed for {node.Schema}.{node.Table}",
                        true, logger);
                    success = false;
                }
            }

            return success;
        }
        finally
        {
            dbm.Close();
        }
    }
}
