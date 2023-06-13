using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

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

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    public DatabaseReCreator(ILogger logger, bool useConsole, DatabaseMigrationParameters databaseMigrationParameters,
        CorrectNewDbParameters correctNewDbParameters, IParametersManager parametersManager) : base(logger, useConsole,
        ActionName, databaseMigrationParameters, parametersManager, ActionDescription)
    {
        _correctNewDbParameters = correctNewDbParameters;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override bool RunAction()
    {
        //წაიშალოს დეველოპერ ბაზა
        var databaseDropper = new DatabaseDropper(Logger, UseConsole, DatabaseMigrationParameters, ParametersManager);
        if (!databaseDropper.Run())
            return false;

        //შეიქმნას თავიდან (სტორედ პროცედურების გათვალისწინებით)
        var databaseMigrationCreator =
            new DatabaseMigrationCreator(Logger, UseConsole, DatabaseMigrationParameters, ParametersManager);
        if (!databaseMigrationCreator.Run())
            return false;

        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
        var correctNewDatabase = new CorrectNewDatabase(Logger, UseConsole, _correctNewDbParameters, ParametersManager);
        return correctNewDatabase.Run();
    }
}