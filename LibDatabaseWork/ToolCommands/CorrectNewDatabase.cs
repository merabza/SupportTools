//Created by ProjectMainClassCreator at 5/9/2021 13:38:34

using System;
using System.Collections.Generic;
using CliParameters;
using DbTools;
using DbToolsFabric;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibDatabaseWork.ToolCommands;

public sealed class CorrectNewDatabase : ToolCommand
{
    private const string ActionName = "Correct new Database";
    private const string ActionDescription = "Correct new Database";

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    public CorrectNewDatabase(ILogger logger, CorrectNewDbParameters correctNewDbParameters,
        IParametersManager? parametersManager) : base(logger, ActionName, correctNewDbParameters, parametersManager,
        ActionDescription)
    {
    }

    private CorrectNewDbParameters CorrectNewDbParameters => (CorrectNewDbParameters)Par;

    private DbManager GetDbManager()
    {
        var dbKit = ManagerFactory.GetKit(EDataProvider.Sql);
        var dbm = DbManager.Create(dbKit, CorrectNewDbParameters.ConnectionString);
        return dbm ?? throw new Exception("Cannot create DbManager");
    }

    protected override bool CheckValidate()
    {
        if (CorrectNewDbParameters.DataProvider != EDataProvider.None)
            return true;

        Logger.LogError("DataProvider not specified");
        return false;
    }

    protected override bool RunAction()
    {
        var constraintsForCorrect = CorrectBitConstraints();

        Logger.LogInformation($"Correction needs {constraintsForCorrect.Count} constraints");

        foreach (var constraintDataModel in constraintsForCorrect)
        {
            Logger.LogInformation($"Correcting table {constraintDataModel.TableName}");

            if (!DeleteConstraint(constraintDataModel))
            {
                Logger.LogError($"Cannot Delete constraint {constraintDataModel.DefaultConstraintName}");
                return false;
            }

            if (CreateConstraint(constraintDataModel))
                continue;

            Logger.LogError($"Cannot Create constraint for table {constraintDataModel.TableName}");
            return false;
        }

        return true;
    }

    private bool CreateConstraint(ConstraintDataModel constraintDataModel)
    {
        return ExecuteCommand(
            $"ALTER TABLE [dbo].[{constraintDataModel.TableName}] ADD CONSTRAINT [{constraintDataModel.DefaultConstraintName}] DEFAULT ((0)) FOR [{constraintDataModel.ColumnName}]");
    }

    private bool DeleteConstraint(ConstraintDataModel constraintDataModel)
    {
        return ExecuteCommand(
            $"ALTER TABLE [dbo].[{constraintDataModel.TableName}] DROP CONSTRAINT [{constraintDataModel.DefaultConstraintName}]");
    }


    public bool ExecuteCommand(string strCommand)
    {
        var dbm = GetDbManager();

        var success = false;

        try
        {
            dbm.Open();
            dbm.ExecuteNonQuery(strCommand);
            success = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, null);
        }
        finally
        {
            dbm.Close();
        }

        return success;
    }


    private List<ConstraintDataModel> CorrectBitConstraints()
    {
        var dbm = GetDbManager();
        try
        {
            var query = @"SELECT t.Name as tableName, c.Name as columnName, dc.Name as defaultConstraintName
FROM sys.tables t
INNER JOIN sys.default_constraints dc ON t.object_id = dc.parent_object_id
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND c.column_id = dc.parent_column_id
INNER JOIN sys.types tt ON c.user_type_id = tt.user_type_id
WHERE tt.name = N'bit'
  AND dc.definition = N'(CONVERT([bit],(0)))'";
            dbm.Open();
            var reader = dbm.ExecuteReader(query);
            var fileNames = new List<ConstraintDataModel>();
            while (reader.Read())
                fileNames.Add(new ConstraintDataModel((string)reader["tableName"],
                    (string)reader["columnName"],
                    (string)reader["defaultConstraintName"]));

            return fileNames;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, true, Logger);
        }
        finally
        {
            dbm.Close();
        }

        return new List<ConstraintDataModel>();
    }
}