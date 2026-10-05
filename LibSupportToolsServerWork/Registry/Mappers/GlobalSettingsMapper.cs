using System;
using ParametersManagement.LibDatabaseParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//გლობალური პარამეტრები (B5): SupportToolsParameters-ის საერთო ზედა დონის ველები და
//DatabasesBackupFilesExchangeParameters ↔ StsGlobalSettingsDataModel. კომპიუტერის ველებს (MachineLocalFields.TopLevel)
//და DatabasesBackupFilesExchangeParameters-ის LocalPath-ს (MachineLocalFields.DatabasesBackupFilesExchange) არ ეხება.
//MediatRLicenseKey საიდუმლოა (G2): გადაიცემა, მაგრამ არსად იბეჭდება
public static class GlobalSettingsMapper
{
    public static StsGlobalSettingsDataModel ToContract(SupportToolsParameters parameters)
    {
        DatabasesBackupFilesExchangeParameters? exchange = parameters.DatabasesBackupFilesExchangeParameters;
        return new StsGlobalSettingsDataModel
        {
            ServiceDescriptionSignature = parameters.ServiceDescriptionSignature,
            UploadTempExtension = parameters.UploadTempExtension,
            ProgramArchiveDateMask = parameters.ProgramArchiveDateMask,
            ProgramArchiveExtension = parameters.ProgramArchiveExtension,
            ParametersFileDateMask = parameters.ParametersFileDateMask,
            ParametersFileExtension = parameters.ParametersFileExtension,
            MediatRLicenseKey = parameters.MediatRLicenseKey,
            FileStorageNameForExchange = parameters.FileStorageNameForExchange,
            SmartSchemaNameForExchange = parameters.SmartSchemaNameForExchange,
            SmartSchemaNameForLocal = parameters.SmartSchemaNameForLocal,
            LocalPackageManagerWebApiClientName = parameters.LocalPackageManagerWebApiClientName,
            DatabasesBackupFilesExchange = new StsDatabasesBackupFilesExchangeDataModel
            {
                DownloadTempExtension = exchange?.DownloadTempExtension,
                UploadTempExtension = exchange?.UploadTempExtension,
                ExchangeFileStorageName = exchange?.ExchangeFileStorageName,
                ExchangeSmartSchemaName = exchange?.ExchangeSmartSchemaName,
                LocalSmartSchemaName = exchange?.LocalSmartSchemaName
            }
        };
    }

    public static void ApplyToLocal(StsGlobalSettingsDataModel contract, SupportToolsParameters parameters)
    {
        parameters.ServiceDescriptionSignature = contract.ServiceDescriptionSignature;
        parameters.UploadTempExtension = contract.UploadTempExtension;
        parameters.ProgramArchiveDateMask = contract.ProgramArchiveDateMask;
        parameters.ProgramArchiveExtension = contract.ProgramArchiveExtension;
        parameters.ParametersFileDateMask = contract.ParametersFileDateMask;
        parameters.ParametersFileExtension = contract.ParametersFileExtension;
        parameters.MediatRLicenseKey = contract.MediatRLicenseKey;
        parameters.FileStorageNameForExchange = contract.FileStorageNameForExchange;
        parameters.SmartSchemaNameForExchange = contract.SmartSchemaNameForExchange;
        parameters.SmartSchemaNameForLocal = contract.SmartSchemaNameForLocal;
        parameters.LocalPackageManagerWebApiClientName = contract.LocalPackageManagerWebApiClientName;

        StsDatabasesBackupFilesExchangeDataModel exchangeContract = contract.DatabasesBackupFilesExchange;
        DatabasesBackupFilesExchangeParameters? exchange = parameters.DatabasesBackupFilesExchangeParameters;
        if (exchange is null)
        {
            //ცარიელი ნაწილისთვის ლოკალური ობიექტი არ იქმნება
            if (IsEmpty(exchangeContract))
            {
                return;
            }

            exchange = new DatabasesBackupFilesExchangeParameters();
            parameters.DatabasesBackupFilesExchangeParameters = exchange;
        }

        //LocalPath ამ კომპიუტერისაა და რჩება
        exchange.DownloadTempExtension = exchangeContract.DownloadTempExtension;
        exchange.UploadTempExtension = exchangeContract.UploadTempExtension;
        exchange.ExchangeFileStorageName = exchangeContract.ExchangeFileStorageName;
        exchange.ExchangeSmartSchemaName = exchangeContract.ExchangeSmartSchemaName;
        exchange.LocalSmartSchemaName = exchangeContract.LocalSmartSchemaName;
    }

    public static StsGlobalSettingsDataModel Normalize(StsGlobalSettingsDataModel contract)
    {
        contract.ServiceDescriptionSignature = ContractNormalization.EmptyToNull(contract.ServiceDescriptionSignature);
        contract.UploadTempExtension = ContractNormalization.EmptyToNull(contract.UploadTempExtension);
        contract.ProgramArchiveDateMask = ContractNormalization.EmptyToNull(contract.ProgramArchiveDateMask);
        contract.ProgramArchiveExtension = ContractNormalization.EmptyToNull(contract.ProgramArchiveExtension);
        contract.ParametersFileDateMask = ContractNormalization.EmptyToNull(contract.ParametersFileDateMask);
        contract.ParametersFileExtension = ContractNormalization.EmptyToNull(contract.ParametersFileExtension);
        contract.MediatRLicenseKey = ContractNormalization.EmptyToNull(contract.MediatRLicenseKey);
        contract.FileStorageNameForExchange = ContractNormalization.EmptyToNull(contract.FileStorageNameForExchange);
        contract.SmartSchemaNameForExchange = ContractNormalization.EmptyToNull(contract.SmartSchemaNameForExchange);
        contract.SmartSchemaNameForLocal = ContractNormalization.EmptyToNull(contract.SmartSchemaNameForLocal);
        contract.LocalPackageManagerWebApiClientName =
            ContractNormalization.EmptyToNull(contract.LocalPackageManagerWebApiClientName);

        StsDatabasesBackupFilesExchangeDataModel exchange = contract.DatabasesBackupFilesExchange;
        exchange.DownloadTempExtension = ContractNormalization.EmptyToNull(exchange.DownloadTempExtension);
        exchange.UploadTempExtension = ContractNormalization.EmptyToNull(exchange.UploadTempExtension);
        exchange.ExchangeFileStorageName = ContractNormalization.EmptyToNull(exchange.ExchangeFileStorageName);
        exchange.ExchangeSmartSchemaName = ContractNormalization.EmptyToNull(exchange.ExchangeSmartSchemaName);
        exchange.LocalSmartSchemaName = ContractNormalization.EmptyToNull(exchange.LocalSmartSchemaName);
        return contract;
    }

    private static bool IsEmpty(StsDatabasesBackupFilesExchangeDataModel exchange)
    {
        string?[] values =
        [
            exchange.DownloadTempExtension, exchange.UploadTempExtension, exchange.ExchangeFileStorageName,
            exchange.ExchangeSmartSchemaName, exchange.LocalSmartSchemaName
        ];
        return Array.TrueForAll(values, string.IsNullOrEmpty);
    }
}
