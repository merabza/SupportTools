using System;
using System.Linq;
using DatabaseTools.DbTools.Models;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;

namespace LibSupportToolsServerWork.Registry.Mappers;

//ბაზის სერვერთან კავშირი: DatabaseServerConnectionData ↔ StsDatabaseServerConnectionDataModel, folders set-ებით.
//DatabaseServerProvider EDatabaseProvider-ის სახელია. ServerUser და ServerPass საიდუმლოა (G2): გადაიცემა, მაგრამ არსად
//იბეჭდება. folders set-ის გზები DB სერვერისაა და არ გარდაიქმნება (README §4.4)
public static class DatabaseServerConnectionMapper
{
    public static StsDatabaseServerConnectionDataModel ToContract(string name, DatabaseServerConnectionData connection)
    {
        return new StsDatabaseServerConnectionDataModel
        {
            Name = name,
            DatabaseServerProvider = connection.DatabaseServerProvider.ToString(),
            DbWebAgentName = connection.DbWebAgentName,
            RemoteDbConnectionName = connection.RemoteDbConnectionName,
            ServerAddress = connection.ServerAddress,
            WindowsNtIntegratedSecurity = connection.WindowsNtIntegratedSecurity,
            ServerUser = connection.ServerUser,
            ServerPass = connection.ServerPass,
            TrustServerCertificate = connection.TrustServerCertificate,
            ConnectionTimeOut = connection.ConnectionTimeOut,
            Encrypt = connection.Encrypt,
            DatabaseFoldersSets =
            [
                .. (connection.DatabaseFoldersSets ?? []).Select(x => new StsDatabaseFoldersSetDataModel
                {
                    Name = x.Key, Backup = x.Value.Backup, Data = x.Value.Data, DataLog = x.Value.DataLog
                })
            ]
        };
    }

    //პროვაიდერი ამ კლიენტისთვის ცნობილი უნდა იყოს (ადაპტერი უცნობს წინასწარ გამორიცხავს). folders set-ები მთლიანად
    //კონტრაქტისაა (G7): dictionary-ის ეგზემპლარი რჩება, შიგთავსი კი ჩანაცვლდება
    public static void ApplyToLocal(StsDatabaseServerConnectionDataModel contract,
        DatabaseServerConnectionData connection)
    {
        connection.DatabaseServerProvider = Enum.Parse<EDatabaseProvider>(contract.DatabaseServerProvider, true);
        connection.DbWebAgentName = contract.DbWebAgentName;
        connection.RemoteDbConnectionName = contract.RemoteDbConnectionName;
        connection.ServerAddress = contract.ServerAddress;
        connection.WindowsNtIntegratedSecurity = contract.WindowsNtIntegratedSecurity;
        connection.ServerUser = contract.ServerUser;
        connection.ServerPass = contract.ServerPass;
        connection.TrustServerCertificate = contract.TrustServerCertificate;
        connection.ConnectionTimeOut = contract.ConnectionTimeOut;
        connection.Encrypt = contract.Encrypt;

        connection.DatabaseFoldersSets ??= [];
        connection.DatabaseFoldersSets.Clear();
        foreach (StsDatabaseFoldersSetDataModel foldersSet in contract.DatabaseFoldersSets)
        {
            connection.DatabaseFoldersSets[foldersSet.Name] = new DatabaseFoldersSet
            {
                Backup = foldersSet.Backup, Data = foldersSet.Data, DataLog = foldersSet.DataLog
            };
        }
    }

    public static StsDatabaseServerConnectionDataModel Normalize(StsDatabaseServerConnectionDataModel contract)
    {
        contract.DatabaseServerProvider =
            ContractNormalization.EnumName<EDatabaseProvider>(contract.DatabaseServerProvider);
        contract.DbWebAgentName = ContractNormalization.EmptyToNull(contract.DbWebAgentName);
        contract.RemoteDbConnectionName = ContractNormalization.EmptyToNull(contract.RemoteDbConnectionName);
        contract.ServerAddress = ContractNormalization.EmptyToNull(contract.ServerAddress);
        contract.ServerUser = ContractNormalization.EmptyToNull(contract.ServerUser);
        contract.ServerPass = ContractNormalization.EmptyToNull(contract.ServerPass);
        foreach (StsDatabaseFoldersSetDataModel foldersSet in contract.DatabaseFoldersSets)
        {
            foldersSet.Backup = ContractNormalization.EmptyToNull(foldersSet.Backup);
            foldersSet.Data = ContractNormalization.EmptyToNull(foldersSet.Data);
            foldersSet.DataLog = ContractNormalization.EmptyToNull(foldersSet.DataLog);
        }

        contract.DatabaseFoldersSets = ContractNormalization.OrderByName(contract.DatabaseFoldersSets, x => x.Name);
        return contract;
    }

    public static bool HasUnknownProvider(StsDatabaseServerConnectionDataModel contract)
    {
        return !ContractNormalization.IsEnumName<EDatabaseProvider>(contract.DatabaseServerProvider);
    }
}
