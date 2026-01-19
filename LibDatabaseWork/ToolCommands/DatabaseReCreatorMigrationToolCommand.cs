using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools.Errors;
using LanguageExt;
using LibDatabaseWork.Models;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.DatabasesManagement;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibDatabaseWork.ToolCommands;

public sealed class DatabaseReCreatorMigrationToolCommand : MigrationToolCommand
{
    private const string ActionName = "Database Recreate";

    private const string ActionDescription = """
                                             This action will do steps:

                                             1. Drop Existing Dev Database (if it exists)
                                             2. Create Initial Migration and create new Dev Database
                                             3. Correct New Database


                                             """;

    private readonly ApiClients _apiClients;

    private readonly CorrectNewDbParameters _correctNewDbParameters;
    private readonly DatabaseServerConnections _databaseServerConnections;
    private readonly DatabaseParameters _devDatabaseParameters;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    public DatabaseReCreatorMigrationToolCommand(ILogger logger,
        DatabaseMigrationParameters databaseMigrationParameters, DatabaseParameters devDatabaseParameters,
        CorrectNewDbParameters correctNewDbParameters, DatabaseServerConnections databaseServerConnections,
        ApiClients apiClients, IHttpClientFactory httpClientFactory, IParametersManager parametersManager) : base(
        logger, ActionName, databaseMigrationParameters, parametersManager, ActionDescription)
    {
        _logger = logger;
        _devDatabaseParameters = devDatabaseParameters;
        _correctNewDbParameters = correctNewDbParameters;
        _databaseServerConnections = databaseServerConnections;
        _apiClients = apiClients;
        _httpClientFactory = httpClientFactory;
    }

    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //დავადგინოთ თუ არსებობს დეველოპერ ბაზა
        var isDatabaseExistsResult =
            await DatabaseMigrationParameters.DatabaseManager.IsDatabaseExists(DatabaseMigrationParameters.DatabaseName,
                cancellationToken);

        if (isDatabaseExistsResult.IsT1)
        {
            _logger.LogInformation("The existence of the base could not be determined");
            return false;
        }

        var dotnetProcessor = new DotnetProcessor(_logger, true);
        dotnetProcessor.Restore(DatabaseMigrationParameters.MigrationProjectFileName);
        dotnetProcessor.Restore(DatabaseMigrationParameters.MigrationStartupProjectFilePath);

        if (isDatabaseExistsResult.AsT0)
        {
            //თუ არსებობს წაიშალოს დეველოპერ ბაზა
            var databaseDropper =
                new DatabaseDropperMigrationToolCommand(_logger, DatabaseMigrationParameters, ParametersManager);
            if (!await databaseDropper.Run(cancellationToken))
                return false;
        }

        //შეიქმნას თავიდან (სტორედ პროცედურების გათვალისწინებით)
        var databaseMigrationCreator =
            new DatabaseMigrationCreatorMigrationToolCommand(_logger, DatabaseMigrationParameters, ParametersManager);
        if (!await databaseMigrationCreator.Run(cancellationToken))
            return false;

        var changeDatabaseRecoveryModelResult = await ChangeDatabaseRecoveryModel(cancellationToken);
        if (changeDatabaseRecoveryModelResult.IsSome)
            _logger.LogError("Error in ChangeDatabaseRecoveryModel");

        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
        var correctNewDatabase = new CorrectNewDatabaseToolCommand(_logger, _correctNewDbParameters, ParametersManager);
        return await correctNewDatabase.Run(cancellationToken);
    }

    private async ValueTask<Option<Err[]>> ChangeDatabaseRecoveryModel(CancellationToken cancellationToken = default)
    {
        var errors = new List<Err>();

        var dbConnectionName = _devDatabaseParameters.DbConnectionName;

        if (string.IsNullOrWhiteSpace(_devDatabaseParameters.DatabaseName))
        {
            _logger.LogError("dev database DbConnectionName is not specified");
            errors.Add(DbToolsErrors.DatabaseConnectionNameIsNotSpecified);
            return errors.ToArray();
        }

        var createDatabaseManagerResult = await DatabaseManagersFactory.CreateDatabaseManager(_logger, true,
            dbConnectionName, _databaseServerConnections, _apiClients, _httpClientFactory, null, null,
            cancellationToken);

        if (createDatabaseManagerResult.IsT1)
        {
            _logger.LogError("Error in CreateDatabaseManager");
            errors.AddRange(createDatabaseManagerResult.AsT1);
        }

        if (string.IsNullOrWhiteSpace(_devDatabaseParameters.DatabaseName))
        {
            _logger.LogError("dev DatabaseName is not specified");
            errors.Add(DbToolsErrors.DevDatabaseNameIsNotSpecified);
        }

        var databaseRecoveryModel = _devDatabaseParameters.DatabaseRecoveryModel ??
                                    DatabaseParameters.DefaultDatabaseRecoveryModel;

        if (errors.Count > 0)
            return errors.ToArray();

        var dbManager = createDatabaseManagerResult.AsT0;

        var changeDatabaseRecoveryModelResult = await dbManager.ChangeDatabaseRecoveryModel(
            _devDatabaseParameters.DatabaseName, databaseRecoveryModel, cancellationToken);

        if (changeDatabaseRecoveryModelResult.IsSome)
            return (Err[])changeDatabaseRecoveryModelResult;
        return null;
    }
}