﻿//using System.Collections;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Threading;
//using System.Threading.Tasks;
//using DatabasesManagement;
//using DbTools;
//using DbTools.Errors;
//using LanguageExt;
//using LibApiClientParameters;
//using LibDatabaseParameters;
//using LibDatabaseWork.Models;
//using LibParameters;
//using Microsoft.Extensions.Logging;
//using SystemToolsShared.Errors;

//// ReSharper disable ConvertToPrimaryConstructor

//namespace LibDatabaseWork.ToolCommands;

//public sealed class DatabaseRecoveryModelChanger : MigrationToolCommand
//{
//    private const string ActionName = "Change Database Recovery model";

//    private const string ActionDescription = @"This action will do steps:

//1. Drop Existing Dev Database
//2. Create Initial Migration and create new Dev Database
//3. Correct New Database

//";

//    private readonly ApiClients _apiClients;

//    private readonly CorrectNewDbParameters _correctNewDbParameters;
//    private readonly DatabaseServerConnections _databaseServerConnections;
//    private readonly DatabaseParameters _devDatabaseParameters;
//    private readonly IHttpClientFactory _httpClientFactory;
//    private readonly ILogger _logger;

//    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
//    public DatabaseReCreator(ILogger logger, DatabaseMigrationParameters databaseMigrationParameters,
//        DatabaseParameters devDatabaseParameters, CorrectNewDbParameters correctNewDbParameters,
//        DatabaseServerConnections databaseServerConnections, ApiClients apiClients,
//        IHttpClientFactory httpClientFactory, IParametersManager parametersManager) : base(logger, ActionName,
//        databaseMigrationParameters, parametersManager, ActionDescription)
//    {
//        _logger = logger;
//        _devDatabaseParameters = devDatabaseParameters;
//        _correctNewDbParameters = correctNewDbParameters;
//        _databaseServerConnections = databaseServerConnections;
//        _apiClients = apiClients;
//        _httpClientFactory = httpClientFactory;
//    }

//    private DatabaseMigrationParameters DatabaseMigrationParameters => (DatabaseMigrationParameters)Par;

//    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
//    {
//        //წაიშალოს დეველოპერ ბაზა
//        var databaseDropper = new DatabaseDropper(_logger, DatabaseMigrationParameters, ParametersManager);
//        if (!await databaseDropper.Run(cancellationToken))
//            return false;

//        //შეიქმნას თავიდან (სტორედ პროცედურების გათვალისწინებით)
//        var databaseMigrationCreator =
//            new DatabaseMigrationCreator(_logger, DatabaseMigrationParameters, ParametersManager);
//        if (!await databaseMigrationCreator.Run(cancellationToken))
//            return false;


//        //გადამოწმდეს ახალი ბაზა და ჩასწორდეს საჭიროების მიხედვით
//        var correctNewDatabase = new CorrectNewDatabase(_logger, _correctNewDbParameters, ParametersManager);
//        return await correctNewDatabase.Run(cancellationToken);
//    }


//    private async ValueTask<Option<IEnumerable<Err>>> ChangeDatabaseRecoveryModel(string databaseName, EDatabaseRecoveryModel EDatabaseRecoveryModel, CancellationToken cancellationToken = default)
//    {
//        var errors = new List<Err>();

//        var dbConnectionName = _devDatabaseParameters.DbConnectionName;

//        if ( string.IsNullOrWhiteSpace(_devDatabaseParameters.DatabaseName))
//        {
//            _logger.LogError("dev database DbConnectionName is not specified");
//            errors.Add(DbToolsErrors.DatabaseConnectionNameIsNotSpecified);
//            return errors;
//        }

//        var createDatabaseManagerResult = await DatabaseManagersFabric.CreateDatabaseManager(_logger, true,
//            dbConnectionName, _databaseServerConnections, _apiClients, _httpClientFactory, null, null,
//            cancellationToken);

//        if ( createDatabaseManagerResult.IsT1)
//        {
//            _logger.LogError("Error in CreateDatabaseManager");
//            errors.AddRange(createDatabaseManagerResult.AsT1);
//        }


//        if ( string.IsNullOrWhiteSpace(_devDatabaseParameters.DatabaseName))
//        {
//            _logger.LogError("dev DatabaseName is not specified");
//            errors.Add(DbToolsErrors.DevDatabaseNameIsNotSpecified);
//        }

//        if ( _devDatabaseParameters.DatabaseRecoveryModel is null)
//        {
//            _logger.LogError("dev DatabaseRecoveryModel is not specified");
//            errors.Add(DbToolsErrors.DevDatabaseRecoveryModelIsNotSpecified);
//        }

//        if (errors.Count > 0)
//            return errors;

//        var dbManager = createDatabaseManagerResult.AsT0;

//        var changeDatabaseRecoveryModelResult =
//            await dbManager.ChangeDatabaseRecoveryModel(_devDatabaseParameters.DatabaseName, _devDatabaseParameters.DatabaseRecoveryModel!.Value, cancellationToken);

//        if (changeDatabaseRecoveryModelResult.IsSome)
//            return (Err[])changeDatabaseRecoveryModelResult;
//        return null;
//    }

//}

