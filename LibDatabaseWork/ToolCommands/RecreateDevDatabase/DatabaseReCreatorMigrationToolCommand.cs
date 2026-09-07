using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DatabaseTools.DbTools;
using DatabaseTools.DbTools.Errors;
using LibDatabaseWork.ToolCommands.CorrectNewDatabase;
using LibDatabaseWork.ToolCommands.CreateDevDatabaseByMigration;
using LibDatabaseWork.ToolCommands.DropDevDatabase;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SystemTools.SharedKernel;
using ToolsManagement.DatabasesManagement;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibDatabaseWork.ToolCommands.RecreateDevDatabase;

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
    private readonly string _appName;

    private readonly CorrectNewDbParameters _correctNewDbParameters;
    private readonly DatabaseServerConnections _databaseServerConnections;
    private readonly DatabaseParameters _devDatabaseParameters;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
    public DatabaseReCreatorMigrationToolCommand(string appName, ILogger logger,
        DatabaseMigrationParameters databaseMigrationParameters, DatabaseParameters devDatabaseParameters,
        CorrectNewDbParameters correctNewDbParameters, DatabaseServerConnections databaseServerConnections,
        ApiClients apiClients, IHttpClientFactory httpClientFactory, IParametersManager parametersManager) : base(
        logger, ActionName, databaseMigrationParameters, parametersManager, ActionDescription)
    {
        _appName = appName;
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
        Result<bool> isDatabaseExistsResult =
            await DatabaseMigrationParameters.DatabaseManager.IsDatabaseExists(DatabaseMigrationParameters.DatabaseName,
                cancellationToken);

        if (isDatabaseExistsResult.IsFailure)
        {
            _logger.LogInformation("The existence of the base could not be determined");
            return false;
        }

        var dotnetProcessor = new DotnetProcessor(_logger, true);
        dotnetProcessor.Restore(DatabaseMigrationParameters.MigrationProjectFileName);
        dotnetProcessor.Restore(DatabaseMigrationParameters.MigrationStartupProjectFilePath);

        if (isDatabaseExistsResult.Value)
        {
            //თუ არსებობს წაიშალოს დეველოპერ ბაზა
            var databaseDropper =
                new DatabaseDropperMigrationToolCommand(_logger, DatabaseMigrationParameters, ParametersManager);
            if (!await databaseDropper.Run(cancellationToken))
            {
                return false;
            }
        }

        //შეიქმნას თავიდან (სტორედ პროცედურების გათვალისწინებით)
        var databaseMigrationCreator =
            new DatabaseMigrationCreatorMigrationToolCommand(_logger, DatabaseMigrationParameters, ParametersManager);
        if (!await databaseMigrationCreator.Run(cancellationToken))
        {
            return false;
        }

        Result changeDatabaseRecoveryModelResult = await ChangeDatabaseRecoveryModel(cancellationToken);
        if (changeDatabaseRecoveryModelResult.IsFailure)
        {
            _logger.LogError("Error in ChangeDatabaseRecoveryModel");
        }

        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
        var correctNewDatabase = new CorrectNewDatabaseToolCommand(_logger, _correctNewDbParameters, ParametersManager);
        return await correctNewDatabase.Run(cancellationToken);
    }

    private async ValueTask<Result> ChangeDatabaseRecoveryModel(CancellationToken cancellationToken = default)
    {
        var errors = new List<Error>();

        string? dbConnectionName = _devDatabaseParameters.DbConnectionName;

        if (string.IsNullOrWhiteSpace(_devDatabaseParameters.DatabaseName))
        {
            _logger.LogError("dev database DbConnectionName is not specified");
            return DbToolsErrors.DatabaseConnectionNameIsNotSpecified;
        }

        Result<IDatabaseManager> createDatabaseManagerResult =
            await DatabaseManagersFactory.CreateDatabaseManager(_appName, _logger, true, dbConnectionName,
                _databaseServerConnections, _apiClients, _httpClientFactory, null, null, cancellationToken);

        if (createDatabaseManagerResult.IsFailure)
        {
            _logger.LogError("Error in CreateDatabaseManager");
            errors.Add(createDatabaseManagerResult.Error);
        }

        if (string.IsNullOrWhiteSpace(_devDatabaseParameters.DatabaseName))
        {
            _logger.LogError("dev DatabaseName is not specified");
            errors.Add(DbToolsErrors.DevDatabaseNameIsNotSpecified);
        }

        EDatabaseRecoveryModel databaseRecoveryModel = _devDatabaseParameters.DatabaseRecoveryModel ??
                                                       DatabaseParameters.DefaultDatabaseRecoveryModel;

        if (errors.Count > 0)
        {
            return Result.CreateValidationError([.. errors]);
        }

        IDatabaseManager dbManager = createDatabaseManagerResult.Value;

        return await dbManager.ChangeDatabaseRecoveryModel(_devDatabaseParameters.DatabaseName, databaseRecoveryModel,
            cancellationToken);
    }
}
