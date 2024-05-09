using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibDatabaseWork.ToolCommands;

public sealed class DatabaseReCreator : MigrationToolCommand
{
    private const string ActionName = "Database Recreate";

    private const string ActionDescription = @"This action will do steps:

1. Drop Existing Dev Database
2. Create Initial Migration and create new Dev Database
3. Correct New Database

";

    private readonly CorrectNewDbParameters _correctNewDbParameters;
    private readonly ILogger _logger;

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    public DatabaseReCreator(ILogger logger, DatabaseMigrationParameters databaseMigrationParameters,
        CorrectNewDbParameters correctNewDbParameters, IParametersManager parametersManager) : base(logger, ActionName,
        databaseMigrationParameters, parametersManager, ActionDescription)
    {
        _logger = logger;
        _correctNewDbParameters = correctNewDbParameters;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        //წაიშალოს დეველოპერ ბაზა
        var databaseDropper = new DatabaseDropper(_logger, DatabaseMigrationParameters, ParametersManager);
        if (!await databaseDropper.Run(cancellationToken))
            return false;

        //შეიქმნას თავიდან (სტორედ პროცედურების გათვალისწინებით)
        var databaseMigrationCreator =
            new DatabaseMigrationCreator(_logger, DatabaseMigrationParameters, ParametersManager);
        if (!await databaseMigrationCreator.Run(cancellationToken))
            return false;

        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
        var correctNewDatabase = new CorrectNewDatabase(_logger, _correctNewDbParameters, ParametersManager);
        return await correctNewDatabase.Run(cancellationToken);
    }
}