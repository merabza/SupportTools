﻿using System.Threading;
using System.Threading.Tasks;
using LibDatabaseWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibDatabaseWork.ToolCommands;

public sealed class DatabaseDropper : MigrationToolCommand
{
    private const string ActionName = "Drop Database";
    private const string ActionDescription = "Drop Database";
    private readonly ILogger _logger;

    //კონსტრუქტორი გამოიყენება, როცა პარამეტრები უნდა ჩაიტვირთოს ფაილიდან
    //public DatabaseDropper(ILogger logger, bool useConsole, ParametersTaskInfo parametersTaskInfo) : base(logger,
    //    useConsole, ActionName, ActionDescription, parametersTaskInfo)
    //{

    //}

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    // ReSharper disable once ConvertToPrimaryConstructor
    public DatabaseDropper(ILogger logger, DatabaseMigrationParameters databaseMigrationParameters,
        IParametersManager? parametersManager) : base(logger, ActionName, databaseMigrationParameters,
        parametersManager, ActionDescription)
    {
        _logger = logger;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        //ბაზის წაშლა
        return Task.FromResult(StShared.RunProcess(true, _logger, "dotnet",
                $"ef database drop --force --context {DatabaseMigrationParameters.DbContextName} --startup-project {DatabaseMigrationParameters.StartupProjectFileName} --project {DatabaseMigrationParameters.MigrationProjectFileName}")
            .IsNone);
    }
}