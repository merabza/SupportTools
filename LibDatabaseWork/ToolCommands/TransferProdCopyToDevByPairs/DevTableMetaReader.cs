using System;
using System.Collections.Generic;
using System.Data;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects;
using LibDatabaseWork.ToolCommands.PairProdCopyAndDevDbObjects.Models;
using LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs.Models;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibDatabaseWork.ToolCommands.TransferProdCopyToDevByPairs;

//Dev-ის runtime metadata: ცხრილების identity და computed სვეტების კომპლექტი
internal static class DevTableMetaReader
{
    public static Dictionary<(string Schema, string Table), DevTableMeta>? Read(string devConnectionString,
        PairedDbObjectsModel pairs, ILogger logger)
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
                                 SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName,
                                        c.is_identity AS IsIdentity, c.is_computed AS IsComputed
                                 FROM sys.columns c
                                 INNER JOIN sys.tables t ON c.object_id = t.object_id
                                 INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                                 """;
            dbm.Open();
            // ReSharper disable once using
            using IDataReader reader = dbm.ExecuteReader(query);

            //pairs-ში არსებული ცხრილების set
            var wanted = new HashSet<(string Schema, string Table)>();
            foreach (PairedTable pt in pairs.PairedTables.Values)
            {
                wanted.Add((pt.DevSchemaName, pt.DevTableName));
            }

            var result = new Dictionary<(string Schema, string Table), DevTableMeta>();
            while (reader.Read())
            {
                string schema = (string)reader["SchemaName"];
                string table = (string)reader["TableName"];
                (string Schema, string Table) key = (schema, table);
                if (!wanted.Contains(key))
                {
                    continue;
                }

                string column = (string)reader["ColumnName"];
                bool isIdentity = (bool)reader["IsIdentity"];
                bool isComputed = (bool)reader["IsComputed"];

                if (!result.TryGetValue(key, out DevTableMeta? meta))
                {
                    meta = new DevTableMeta();
                    result[key] = meta;
                }

                if (isIdentity)
                {
                    meta.IdentityColumns.Add(column);
                }

                if (isComputed)
                {
                    meta.ComputedColumns.Add(column);
                }
            }

            //დარწმუნდი, რომ ყველა paired Dev ცხრილისთვის გვაქვს ჩანაწერი (თუნდაც ცარიელი identity/computed-ით)
            foreach ((string Schema, string Table) key in wanted)
            {
                if (!result.ContainsKey(key))
                {
                    result[key] = new DevTableMeta();
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, "Failed to read Dev table metadata", true, logger);
            return null;
        }
        finally
        {
            dbm.Close();
        }
    }
}
