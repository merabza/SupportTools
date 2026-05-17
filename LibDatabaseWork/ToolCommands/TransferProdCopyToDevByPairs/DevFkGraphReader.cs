using System;
using System.Collections.Generic;
using System.Data;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//Dev ბაზიდან foreign-key დამოკიდებულებების ამოღება ცხრილების დონეზე
internal static class DevFkGraphReader
{
    public static List<FkEdge>? Read(string devConnectionString, ILogger logger)
    {
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        using var dbm = DbManager.Create(dbKit, devConnectionString);
        if (dbm is null)
        {
            StShared.WriteErrorLine("Cannot create DbManager for Dev database", true, logger);
            return null;
        }

        try
        {
            const string query = """
                                 SELECT DISTINCT
                                     fs.name AS FromSchema, ft.name AS FromTable,
                                     ts.name AS ToSchema, tt.name AS ToTable
                                 FROM sys.foreign_keys fk
                                 INNER JOIN sys.tables ft ON fk.parent_object_id = ft.object_id
                                 INNER JOIN sys.schemas fs ON ft.schema_id = fs.schema_id
                                 INNER JOIN sys.tables tt ON fk.referenced_object_id = tt.object_id
                                 INNER JOIN sys.schemas ts ON tt.schema_id = ts.schema_id
                                 """;
            dbm.Open();
            // ReSharper disable once using
            using IDataReader reader = dbm.ExecuteReader(query);
            var edges = new List<FkEdge>();
            while (reader.Read())
            {
                edges.Add(new FkEdge((string)reader["FromSchema"], (string)reader["FromTable"],
                    (string)reader["ToSchema"], (string)reader["ToTable"]));
            }

            return edges;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, "Failed to read Dev foreign-key graph", true, logger);
            return null;
        }
        finally
        {
            dbm.Close();
        }
    }
}
