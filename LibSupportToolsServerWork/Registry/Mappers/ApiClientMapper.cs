using ParametersManagement.LibApiClientParameters;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//API კლიენტი: ApiClientSettings ↔ StsApiClientDataModel. ApiKey საიდუმლოა (G2): გადაიცემა, მაგრამ არსად იბეჭდება
public static class ApiClientMapper
{
    public static StsApiClientDataModel ToContract(string name, ApiClientSettings apiClient)
    {
        return new StsApiClientDataModel { Name = name, Server = apiClient.Server, ApiKey = apiClient.ApiKey };
    }

    public static void ApplyToLocal(StsApiClientDataModel contract, ApiClientSettings apiClient)
    {
        apiClient.Server = contract.Server;
        apiClient.ApiKey = contract.ApiKey;
    }

    public static StsApiClientDataModel Normalize(StsApiClientDataModel contract)
    {
        contract.Server = ContractNormalization.EmptyToNull(contract.Server);
        contract.ApiKey = ContractNormalization.EmptyToNull(contract.ApiKey);
        return contract;
    }
}
