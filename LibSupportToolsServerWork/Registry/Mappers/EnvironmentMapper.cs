using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//გარემო: SupportToolsParameters.Environments-ის ჩანაწერი (სახელი → აღწერა) ↔ StsEnvironmentDataModel
public static class EnvironmentMapper
{
    public static StsEnvironmentDataModel ToContract(string name, string? description)
    {
        return new StsEnvironmentDataModel { Name = name, Description = description };
    }

    //ლოკალური dictionary-ის მნიშვნელობა null არ არის
    public static string ToLocal(StsEnvironmentDataModel contract)
    {
        return contract.Description ?? string.Empty;
    }

    public static StsEnvironmentDataModel Normalize(StsEnvironmentDataModel contract)
    {
        contract.Description = ContractNormalization.EmptyToNull(contract.Description);
        return contract;
    }
}
