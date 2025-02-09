using System.IO;
using CliParameters;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibDatabaseWork.ToolCommands;

public /*open*/ class MigrationToolCommand : ToolCommand
{
    private readonly ILogger _logger;

    protected MigrationToolCommand(ILogger logger, string actionName,
        DatabaseMigrationParameters databaseMigrationParameters, IParametersManager? parametersManager,
        string actionDescription) : base(logger, actionName, databaseMigrationParameters, parametersManager,
        actionDescription)
    {
        _logger = logger;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override bool CheckValidate()
    {
        //გამშვები პროექტის ფაილის გზა მითითებული უნდა იყოს.
        if (string.IsNullOrWhiteSpace(DatabaseMigrationParameters.MigrationStartupProjectFilePath) ||
            !File.Exists(DatabaseMigrationParameters.MigrationStartupProjectFilePath))
        {
            _logger.LogError("MigrationStartup Project file name not Specified or not found");
            return false;
        }

        //მიგრაციის პროექტის ფაილის გზა მითითებული უნდა იყოს.
        if (string.IsNullOrWhiteSpace(DatabaseMigrationParameters.MigrationProjectFileName) ||
            !File.Exists(DatabaseMigrationParameters.MigrationProjectFileName))
        {
            _logger.LogError("Migration Project file name not Specified or not found");
            return false;
        }

        //ბაზის კონტექსტის სახელი მითითებული უნდა იყოს.
        if (!string.IsNullOrWhiteSpace(DatabaseMigrationParameters.DbContextName))
            return true;

        _logger.LogError("Database Context Name not Specified");
        return false;
    }
}