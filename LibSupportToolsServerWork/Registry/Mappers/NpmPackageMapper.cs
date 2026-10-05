using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//npm-ის პაკეტი: SupportToolsParameters.NpmPackages-ის ჩანაწერი (სახელი → აღწერა) ↔ StsNpmPackageDataModel
public static class NpmPackageMapper
{
    public static StsNpmPackageDataModel ToContract(string name, string? description)
    {
        return new StsNpmPackageDataModel { Name = name, Description = description };
    }

    //ლოკალური dictionary-ის მნიშვნელობა null არ არის
    public static string ToLocal(StsNpmPackageDataModel contract)
    {
        return contract.Description ?? string.Empty;
    }

    public static StsNpmPackageDataModel Normalize(StsNpmPackageDataModel contract)
    {
        contract.Description = ContractNormalization.EmptyToNull(contract.Description);
        return contract;
    }
}
