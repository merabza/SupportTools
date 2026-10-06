using System;
using DatabaseTools.DbTools;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//ბაზის პარამეტრები (B6/B7): DatabaseParameters ↔ StsDatabaseParametersDataModel. პროექტის DevDatabaseParameters და
//ProdCopyDatabaseParameters, ასევე ServerInfo-ს CurrentDatabaseParameters და NewDatabaseParameters. null ნიშნავს, რომ
//პარამეტრები არ არის. DatabaseRecoveryModel და BackupType EDatabaseRecoveryModel-ისა და EBackupType-ის სახელებია
public static class DatabaseParametersMapper
{
    public static StsDatabaseParametersDataModel? ToContract(DatabaseParameters? parameters)
    {
        if (parameters is null)
        {
            return null;
        }

        return new StsDatabaseParametersDataModel
        {
            DbConnectionName = parameters.DbConnectionName,
            DatabaseRecoveryModel = parameters.DatabaseRecoveryModel?.ToString(),
            DbServerFoldersSetName = parameters.DbServerFoldersSetName,
            DatabaseName = parameters.DatabaseName,
            SmartSchemaName = parameters.SmartSchemaName,
            FileStorageName = parameters.FileStorageName,
            CommandTimeOut = parameters.CommandTimeOut,
            SkipBackupBeforeRestore = parameters.SkipBackupBeforeRestore,
            BackupNamePrefix = parameters.BackupNamePrefix,
            DateMask = parameters.DateMask,
            BackupFileExtension = parameters.BackupFileExtension,
            BackupNameMiddlePart = parameters.BackupNameMiddlePart,
            Compress = parameters.Compress,
            Verify = parameters.Verify,
            BackupType = parameters.BackupType?.ToString()
        };
    }

    //enum-ების სახელები ამ კლიენტისთვის ცნობილი უნდა იყოს (ადაპტერი უცნობს წინასწარ გამორიცხავს)
    public static void ApplyToLocal(StsDatabaseParametersDataModel contract, DatabaseParameters parameters)
    {
        parameters.DbConnectionName = contract.DbConnectionName;
        parameters.DatabaseRecoveryModel = contract.DatabaseRecoveryModel is null
            ? null
            : Enum.Parse<EDatabaseRecoveryModel>(contract.DatabaseRecoveryModel, true);
        parameters.DbServerFoldersSetName = contract.DbServerFoldersSetName;
        parameters.DatabaseName = contract.DatabaseName;
        parameters.SmartSchemaName = contract.SmartSchemaName;
        parameters.FileStorageName = contract.FileStorageName;
        parameters.CommandTimeOut = contract.CommandTimeOut;
        parameters.SkipBackupBeforeRestore = contract.SkipBackupBeforeRestore;
        parameters.BackupNamePrefix = contract.BackupNamePrefix;
        parameters.DateMask = contract.DateMask;
        parameters.BackupFileExtension = contract.BackupFileExtension;
        parameters.BackupNameMiddlePart = contract.BackupNameMiddlePart;
        parameters.Compress = contract.Compress;
        parameters.Verify = contract.Verify;
        parameters.BackupType = contract.BackupType is null ? null : Enum.Parse<EBackupType>(contract.BackupType, true);
    }

    //contract null-ია, თუ პარამეტრები არ არის. existing: ლოკალური ეგზემპლარი, რომელიც ადგილზე ახლდება
    public static DatabaseParameters? ToLocal(StsDatabaseParametersDataModel? contract, DatabaseParameters? existing)
    {
        if (contract is null)
        {
            return null;
        }

        DatabaseParameters parameters = existing ?? new DatabaseParameters();
        ApplyToLocal(contract, parameters);
        return parameters;
    }

    public static StsDatabaseParametersDataModel? Normalize(StsDatabaseParametersDataModel? contract)
    {
        if (contract is null)
        {
            return null;
        }

        contract.DbConnectionName = ContractNormalization.EmptyToNull(contract.DbConnectionName);
        contract.DatabaseRecoveryModel = contract.DatabaseRecoveryModel is null
            ? null
            : ContractNormalization.EnumName<EDatabaseRecoveryModel>(contract.DatabaseRecoveryModel);
        contract.DbServerFoldersSetName = ContractNormalization.EmptyToNull(contract.DbServerFoldersSetName);
        contract.DatabaseName = ContractNormalization.EmptyToNull(contract.DatabaseName);
        contract.SmartSchemaName = ContractNormalization.EmptyToNull(contract.SmartSchemaName);
        contract.FileStorageName = ContractNormalization.EmptyToNull(contract.FileStorageName);
        contract.BackupNamePrefix = ContractNormalization.EmptyToNull(contract.BackupNamePrefix);
        contract.DateMask = ContractNormalization.EmptyToNull(contract.DateMask);
        contract.BackupFileExtension = ContractNormalization.EmptyToNull(contract.BackupFileExtension);
        contract.BackupNameMiddlePart = ContractNormalization.EmptyToNull(contract.BackupNameMiddlePart);
        contract.BackupType = contract.BackupType is null
            ? null
            : ContractNormalization.EnumName<EBackupType>(contract.BackupType);
        return contract;
    }

    //ამ კლიენტისთვის უცნობი enum-ის ველის სახელი; null — ასეთი ველი არ არის
    public static string? FindUnknownEnumField(StsDatabaseParametersDataModel? contract)
    {
        if (contract?.DatabaseRecoveryModel is not null &&
            !ContractNormalization.IsEnumName<EDatabaseRecoveryModel>(contract.DatabaseRecoveryModel))
        {
            return nameof(StsDatabaseParametersDataModel.DatabaseRecoveryModel);
        }

        return contract?.BackupType is not null && !ContractNormalization.IsEnumName<EBackupType>(contract.BackupType)
            ? nameof(StsDatabaseParametersDataModel.BackupType)
            : null;
    }
}
