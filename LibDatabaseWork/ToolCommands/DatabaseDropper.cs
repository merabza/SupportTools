using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibDatabaseWork.ToolCommands;

public sealed class DatabaseDropper : MigrationToolCommand
{
    private const string ActionName = "Drop Database";
    private const string ActionDescription = "Drop Database";

    //კონსტრუქტორი გამოიყენება, როცა პარამეტრები უნდა ჩაიტვირთოს ფაილიდან
    //public DatabaseDropper(ILogger logger, bool useConsole, ParametersTaskInfo parametersTaskInfo) : base(logger,
    //    useConsole, ActionName, ActionDescription, parametersTaskInfo)
    //{

    //}

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    public DatabaseDropper(ILogger logger, bool useConsole, DatabaseMigrationParameters databaseMigrationParameters,
        IParametersManager? parametersManager) : base(logger, useConsole, ActionName, databaseMigrationParameters,
        parametersManager, ActionDescription)
    {
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override bool RunAction()
    {
        //ბაზის წაშლა
        return StShared.RunProcess(true, Logger, "dotnet",
            $"ef database drop --force --context {DatabaseMigrationParameters.DbContextName} --startup-project {DatabaseMigrationParameters.StartupProjectFileName} --project {DatabaseMigrationParameters.MigrationProjectFileName}");
    }
}