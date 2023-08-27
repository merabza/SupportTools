using System.IO;
using CliParameters;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.ToolCommands;

public /*open*/ class MigrationToolCommand : ToolCommand
{
    protected MigrationToolCommand(ILogger logger, string actionName,
        DatabaseMigrationParameters databaseMigrationParameters, IParametersManager? parametersManager,
        string? actionDescription = null) : base(logger, actionName, databaseMigrationParameters, parametersManager,
        actionDescription)
    {
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override bool CheckValidate()
    {
        //გამშვები პროექტის ფაილის გზა მითითებული უნდა იყოს.
        if (string.IsNullOrWhiteSpace(DatabaseMigrationParameters.StartupProjectFileName) ||
            !File.Exists(DatabaseMigrationParameters.StartupProjectFileName))
        {
            Logger.LogError("Main Or Seed Project file name not Specified or not found");
            return false;
        }

        //მიგრაციის პროექტის ფაილის გზა მითითებული უნდა იყოს.
        if (string.IsNullOrWhiteSpace(DatabaseMigrationParameters.MigrationProjectFileName) ||
            !File.Exists(DatabaseMigrationParameters.MigrationProjectFileName))
        {
            Logger.LogError("Migration Project file name not Specified or not found");
            return false;
        }

        //ბაზის კონტექსტის სახელი მითითებული უნდა იყოს.
        if (!string.IsNullOrWhiteSpace(DatabaseMigrationParameters.DbContextName))
            return true;

        Logger.LogError("Database Context Name not Specified");
        return false;
    }
}