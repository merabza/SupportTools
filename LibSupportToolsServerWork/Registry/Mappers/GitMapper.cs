using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.Registry.Mappers;

//git რეპოზიტორია: GitDataModel ↔ StsGitDataModel. GitProjectFolderName შეფარდებითი გზაა და შეიძლება შეიცავდეს
//{SpaProjectFolderRelativePath}-ს: prefix-ით არ გარდაიქმნება, Linux-ზე მხოლოდ გამყოფები იცვლება (README §4.4)
public static class GitMapper
{
    //კონტრაქტის ველები სავალდებულოა: ცარიელ მნიშვნელობას სერვერი ვალიდაციით უარყოფს
    public static StsGitDataModel ToContract(string name, GitDataModel git, PathMapper pathMapper)
    {
        return new StsGitDataModel
        {
            GitProjectName = name,
            GitProjectAddress = git.GitProjectAddress ?? string.Empty,
            GitProjectFolderName =
                pathMapper.NormalizeRelativeToCanonical(git.GitProjectFolderName) ?? string.Empty,
            GitIgnorePatternName = git.GitIgnorePatternName ?? string.Empty
        };
    }

    public static void ApplyToLocal(StsGitDataModel contract, GitDataModel git, PathMapper pathMapper)
    {
        git.GitProjectAddress = contract.GitProjectAddress;
        git.GitProjectFolderName = pathMapper.NormalizeRelativeToLocal(contract.GitProjectFolderName);
        git.GitIgnorePatternName = contract.GitIgnorePatternName;
    }

    //ყველა ველი სავალდებულოა, ამიტომ ნორმალიზაცია არაფერს ცვლის
    public static StsGitDataModel Normalize(StsGitDataModel contract)
    {
        return contract;
    }
}
