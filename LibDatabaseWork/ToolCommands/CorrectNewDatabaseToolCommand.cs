//Created by ProjectMainClassCreator at 5/9/2021 13:38:34

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using DatabaseTools.DbTools;
using DatabaseTools.DbToolsFactory;
using LibDatabaseWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

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
        DbKit dbKit = DbKitFactory.GetKit(EDatabaseProvider.SqlServer);
        // ReSharper disable once using
        return DbManager.Create(dbKit, CorrectNewDbParameters.ConnectionString) ??
               throw new Exception("Cannot create DbManager");
    }

    protected override bool CheckValidate()
    {
        if (CorrectNewDbParameters.DataProvider != EDatabaseProvider.None)
        {
            return true;
        }

        _logger.LogError("DataProvider not specified");
        return false;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        List<ConstraintDataModel> constraintsForCorrect = CorrectBitConstraints();

        int constraintsForCorrectCount = constraintsForCorrect.Count;
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Correction needs {ConstraintsForCorrectCount} constraints",
                constraintsForCorrectCount);
        }

        foreach (ConstraintDataModel constraintDataModel in constraintsForCorrect)
        {
            string tableName = constraintDataModel.TableName;
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Correcting table {TableName}", tableName);
            }

            if (!DeleteConstraint(constraintDataModel))
            {
                string defaultConstraintName = constraintDataModel.DefaultConstraintName;
                _logger.LogError("Cannot Delete constraint {DefaultConstraintName}", defaultConstraintName);
                return ValueTask.FromResult(false);
            }

            if (CreateConstraint(constraintDataModel))
            {
                continue;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogError("Cannot Create constraint for table {TableName}", tableName);
            }

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
        using DbManager dbm = GetDbManager();

        bool success = false;

        try
        {
            dbm.Open();
            dbm.ExecuteNonQuery(strCommand);
            success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when execute command {StrCommand}", strCommand);
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

        using DbManager dbm = GetDbManager();
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
            using IDataReader reader = dbm.ExecuteReader(query);
            var fileNames = new List<ConstraintDataModel>();
            while (reader.Read())
            {
                fileNames.Add(new ConstraintDataModel((string)reader["tableName"], (string)reader["columnName"],
                    (string)reader["defaultConstraintName"]));
            }

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
