using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//სერვერი: ServerDataModel ↔ StsServerDataModel. IsLocal ამ კომპიუტერისაა (G6, MachineLocalFields.Server): კონტრაქტში
//არ მიდის და ApplyToLocal მას არ ეხება. ServerSideDownloadFolder და ServerSideDeployFolder სამიზნე სერვერის გზებია და
//არ გარდაიქმნება (README §4.4)
public static class ServerMapper
{
    public static StsServerDataModel ToContract(string name, ServerDataModel server)
    {
        return new StsServerDataModel
        {
            Name = name,
            WebAgentName = server.WebAgentName,
            WebAgentInstallerName = server.WebAgentInstallerName,
            FilesUserName = server.FilesUserName,
            FilesUsersGroupName = server.FilesUsersGroupName,
            Runtime = server.Runtime,
            ServerSideDownloadFolder = server.ServerSideDownloadFolder,
            ServerSideDeployFolder = server.ServerSideDeployFolder
        };
    }

    public static void ApplyToLocal(StsServerDataModel contract, ServerDataModel server)
    {
        server.WebAgentName = contract.WebAgentName;
        server.WebAgentInstallerName = contract.WebAgentInstallerName;
        server.FilesUserName = contract.FilesUserName;
        server.FilesUsersGroupName = contract.FilesUsersGroupName;
        server.Runtime = contract.Runtime;
        server.ServerSideDownloadFolder = contract.ServerSideDownloadFolder;
        server.ServerSideDeployFolder = contract.ServerSideDeployFolder;
    }

    public static StsServerDataModel Normalize(StsServerDataModel contract)
    {
        contract.WebAgentName = ContractNormalization.EmptyToNull(contract.WebAgentName);
        contract.WebAgentInstallerName = ContractNormalization.EmptyToNull(contract.WebAgentInstallerName);
        contract.FilesUserName = ContractNormalization.EmptyToNull(contract.FilesUserName);
        contract.FilesUsersGroupName = ContractNormalization.EmptyToNull(contract.FilesUsersGroupName);
        contract.Runtime = ContractNormalization.EmptyToNull(contract.Runtime);
        contract.ServerSideDownloadFolder = ContractNormalization.EmptyToNull(contract.ServerSideDownloadFolder);
        contract.ServerSideDeployFolder = ContractNormalization.EmptyToNull(contract.ServerSideDeployFolder);
        return contract;
    }
}
