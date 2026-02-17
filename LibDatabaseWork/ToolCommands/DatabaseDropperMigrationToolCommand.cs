using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.Models;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibDatabaseWork.ToolCommands;

public sealed class DatabaseDropperMigrationToolCommand : MigrationToolCommand
{
    private const string ActionName = "Drop Database";
    private const string ActionDescription = "Drop Database";
    private readonly ILogger _logger;

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseDropperMigrationToolCommand(ILogger logger, DatabaseMigrationParameters databaseMigrationParameters,
        IParametersManager? parametersManager) : base(logger, ActionName, databaseMigrationParameters,
        parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var dotnetProcessor = new DotnetProcessor(_logger, true);

        //ბაზის წაშლა
        return ValueTask.FromResult(dotnetProcessor.EfDropDatabase(DatabaseMigrationParameters.DbContextName,
            DatabaseMigrationParameters.MigrationStartupProjectFilePath,
            DatabaseMigrationParameters.MigrationProjectFileName).IsNone);
    }
}
