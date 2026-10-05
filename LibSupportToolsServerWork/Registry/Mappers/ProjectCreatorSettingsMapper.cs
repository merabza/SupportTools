using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//პროექტის შემქმნელის პარამეტრები (B5): AppProjectCreatorAllParameters, Templates-ის გარეშე (შაბლონები ცალკე
//კოლექციაა) ↔ StsProjectCreatorSettingsDataModel. ProjectsFolderPathReal და SecretsFolderPathReal PathMapper-ით
//გარდაიქმნება (README §4.4)
public static class ProjectCreatorSettingsMapper
{
    //ლოკალური ობიექტი, რომელიც ჯერ არ არსებობს, ცარიელ კონტრაქტს იძლევა
    public static StsProjectCreatorSettingsDataModel ToContract(AppProjectCreatorAllParameters? parameters,
        PathMapper pathMapper)
    {
        if (parameters is null)
        {
            return new StsProjectCreatorSettingsDataModel();
        }

        return new StsProjectCreatorSettingsDataModel
        {
            IndentSize = parameters.IndentSize,
            FakeHostProjectName = parameters.FakeHostProjectName,
            ProjectsFolderPathReal = pathMapper.ToCanonical(parameters.ProjectsFolderPathReal),
            SecretsFolderPathReal = pathMapper.ToCanonical(parameters.SecretsFolderPathReal),
            ProductionServerName = parameters.ProductionServerName,
            ProductionEnvironmentName = parameters.ProductionEnvironmentName,
            DeveloperDbConnectionName = parameters.DeveloperDbConnectionName,
            DatabaseExchangeFileStorageName = parameters.DatabaseExchangeFileStorageName,
            UseSmartSchema = parameters.UseSmartSchema
        };
    }

    //Templates-ს არ ეხება
    public static void ApplyToLocal(StsProjectCreatorSettingsDataModel contract,
        AppProjectCreatorAllParameters parameters, PathMapper pathMapper)
    {
        parameters.IndentSize = contract.IndentSize;
        parameters.FakeHostProjectName = contract.FakeHostProjectName;
        parameters.ProjectsFolderPathReal = pathMapper.ToLocal(contract.ProjectsFolderPathReal);
        parameters.SecretsFolderPathReal = pathMapper.ToLocal(contract.SecretsFolderPathReal);
        parameters.ProductionServerName = contract.ProductionServerName;
        parameters.ProductionEnvironmentName = contract.ProductionEnvironmentName;
        parameters.DeveloperDbConnectionName = contract.DeveloperDbConnectionName;
        parameters.DatabaseExchangeFileStorageName = contract.DatabaseExchangeFileStorageName;
        parameters.UseSmartSchema = contract.UseSmartSchema;
    }

    public static StsProjectCreatorSettingsDataModel Normalize(StsProjectCreatorSettingsDataModel contract)
    {
        contract.FakeHostProjectName = ContractNormalization.EmptyToNull(contract.FakeHostProjectName);
        contract.ProjectsFolderPathReal =
            ContractNormalization.UpperDriveLetter(ContractNormalization.EmptyToNull(contract.ProjectsFolderPathReal));
        contract.SecretsFolderPathReal =
            ContractNormalization.UpperDriveLetter(ContractNormalization.EmptyToNull(contract.SecretsFolderPathReal));
        contract.ProductionServerName = ContractNormalization.EmptyToNull(contract.ProductionServerName);
        contract.ProductionEnvironmentName = ContractNormalization.EmptyToNull(contract.ProductionEnvironmentName);
        contract.DeveloperDbConnectionName = ContractNormalization.EmptyToNull(contract.DeveloperDbConnectionName);
        contract.DatabaseExchangeFileStorageName =
            ContractNormalization.EmptyToNull(contract.DatabaseExchangeFileStorageName);
        contract.UseSmartSchema = ContractNormalization.EmptyToNull(contract.UseSmartSchema);
        return contract;
    }
}
