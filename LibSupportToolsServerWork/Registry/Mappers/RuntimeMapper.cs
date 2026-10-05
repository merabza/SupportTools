using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//Runtime Identifier: SupportToolsParameters.RunTimes-ის ჩანაწერი (სახელი → აღწერა) ↔ StsRuntimeDataModel
public static class RuntimeMapper
{
    public static StsRuntimeDataModel ToContract(string name, string? description)
    {
        return new StsRuntimeDataModel { Name = name, Description = description };
    }

    //ლოკალური dictionary-ის მნიშვნელობა null არ არის
    public static string ToLocal(StsRuntimeDataModel contract)
    {
        return contract.Description ?? string.Empty;
    }

    public static StsRuntimeDataModel Normalize(StsRuntimeDataModel contract)
    {
        contract.Description = ContractNormalization.EmptyToNull(contract.Description);
        return contract;
    }
}
