//Created by ProjectMainClassCreator at 5/9/2021 13:38:34

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using DbTools;
using DbToolsFabric;
using LibDatabaseParameters;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibDatabaseWork.ToolCommands;

public sealed class CorrectNewDatabaseToolCommand : ToolCommand
{
    private const string ActionName = "Correct new Database";
    private const string ActionDescription = "Correct new Database";
    private readonly ILogger _logger;

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    // ReSharper disable once ConvertToPrimaryConstructor
    public CorrectNewDatabaseToolCommand(ILogger logger, CorrectNewDbParameters correctNewDbParameters,
        IParametersManager? parametersManager) : base(logger, ActionName, correctNewDbParameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
    }

    private CorrectNewDbParameters CorrectNewDbParameters => (CorrectNewDbParameters)Par;

    private DbManager GetDbManager()
    {
        var dbKit = ManagerFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        return DbManager.Create(dbKit, CorrectNewDbParameters.ConnectionString) ??
               throw new Exception("Cannot create DbManager");
    }

    protected override bool CheckValidate()
    {
        if (CorrectNewDbParameters.DataProvider != EDatabaseProvider.None)
            return true;

        _logger.LogError("DataProvider not specified");
        return false;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var constraintsForCorrect = CorrectBitConstraints();

        var constraintsForCorrectCount = constraintsForCorrect.Count;
        _logger.LogInformation("Correction needs {constraintsForCorrectCount} constraints", constraintsForCorrectCount);

        foreach (var constraintDataModel in constraintsForCorrect)
        {
            var tableName = constraintDataModel.TableName;
            _logger.LogInformation("Correcting table {tableName}", tableName);

            if (!DeleteConstraint(constraintDataModel))
            {
                var defaultConstraintName = constraintDataModel.DefaultConstraintName;
                _logger.LogError("Cannot Delete constraint {defaultConstraintName}", defaultConstraintName);
                return ValueTask.FromResult(false);
            }

            if (CreateConstraint(constraintDataModel))
                continue;

            _logger.LogError("Cannot Create constraint for table {tableName}", tableName);
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
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


    private bool ExecuteCommand(string strCommand)
    {
        // ReSharper disable once using
        using var dbm = GetDbManager();

        var success = false;

        try
        {
            dbm.Open();
            dbm.ExecuteNonQuery(strCommand);
            success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when execute command {strCommand}", strCommand);
        }
        finally
        {
            dbm.Close();
        }

        return success;
    }


    private List<ConstraintDataModel> CorrectBitConstraints()
    {
        // ReSharper disable once using

        using var dbm = GetDbManager();
        try
        {
            const string query = """
                                 SELECT t.Name as tableName, c.Name as columnName, dc.Name as defaultConstraintName
                                 FROM sys.tables t
                                 INNER JOIN sys.default_constraints dc ON t.object_id = dc.parent_object_id
                                 INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND c.column_id = dc.parent_column_id
                                 INNER JOIN sys.types tt ON c.user_type_id = tt.user_type_id
                                 WHERE tt.name = N'bit'
                                   AND dc.definition = N'(CONVERT([bit],(0)))'
                                 """;
            dbm.Open();
            // ReSharper disable once using
            using var reader = dbm.ExecuteReader(query);
            var fileNames = new List<ConstraintDataModel>();
            while (reader.Read())
                fileNames.Add(new ConstraintDataModel((string)reader["tableName"], (string)reader["columnName"],
                    (string)reader["defaultConstraintName"]));

            return fileNames;
        }
        catch (Exception ex)
        {
            StShared.WriteException(ex, true, _logger);
        }
        finally
        {
            dbm.Close();
        }

        return [];
    }
}