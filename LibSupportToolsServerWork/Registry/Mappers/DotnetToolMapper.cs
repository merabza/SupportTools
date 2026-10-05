using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//dotnet-ის ხელსაწყო: DotnetToolData ↔ StsDotnetToolDataModel, მხოლოდ საერთო ველებით (PackageId, MaxVersion,
//Description). InstalledVersion, LatestVersion და CommandName ამ კომპიუტერისაა (MachineLocalFields.DotnetTool):
//კონტრაქტში არ მიდის და ApplyToLocal მათ არ ეხება
public static class DotnetToolMapper
{
    public static StsDotnetToolDataModel ToContract(string name, DotnetToolData dotnetTool)
    {
        return new StsDotnetToolDataModel
        {
            Name = name,
            //სერვერზე სავალდებულოა: PackageId-ის გარეშე ჩანაწერს სერვერი ვალიდაციით უარყოფს
            PackageId = dotnetTool.PackageId ?? string.Empty,
            MaxVersion = dotnetTool.MaxVersion,
            Description = dotnetTool.Description
        };
    }

    public static void ApplyToLocal(StsDotnetToolDataModel contract, DotnetToolData dotnetTool)
    {
        dotnetTool.PackageId = contract.PackageId;
        dotnetTool.MaxVersion = contract.MaxVersion;
        dotnetTool.Description = contract.Description;
    }

    public static StsDotnetToolDataModel Normalize(StsDotnetToolDataModel contract)
    {
        contract.MaxVersion = ContractNormalization.EmptyToNull(contract.MaxVersion);
        contract.Description = ContractNormalization.EmptyToNull(contract.Description);
        return contract;
    }
}
