using LibGitWork.Models;
using SupportToolsServerApiContracts.Models;

namespace LibGitWork.Mappers;

public static class GitMapper
{
    public static StsGitDataModel ToContractModel(this GitData gitData)
    {
        return new StsGitDataModel
        {
            GitProjectName = gitData.GitProjectName,
            GitProjectAddress = gitData.GitProjectAddress,
            GitProjectFolderName = gitData.GitProjectFolderName,
            GitIgnorePatternName = gitData.GitIgnorePatternName
        };
    }
}
