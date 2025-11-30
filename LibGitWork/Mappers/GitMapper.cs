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
            GitIgnorePathName = gitData.GitIgnorePathName
        };
    }

    //public static StsGitDataModel ToContractModel(this GitData gitData)
    //{
    //    return new StsGitDataModel
    //    {
    //        GitProjectName = gitData.GdName,
    //        GitProjectAddress = gitData.GdGitAddress,
    //        GitProjectFolderName = gitData.GdFolderName,
    //        GitIgnorePathName = gitData.GitIgnoreFileTypeNavigation.Name
    //    };
    //}

    //public static GitDataForSave AdaptTo(this StsGitDataModel gitRepoModel)
    //{
    //    return new GitDataForSave
    //    {
    //        GitProjectName = gitRepoModel.GitProjectName,
    //        GitProjectAddress = gitRepoModel.GitProjectAddress,
    //        GitProjectFolderName = gitRepoModel.GitProjectFolderName,
    //        GitIgnorePathName = gitRepoModel.GitIgnorePathName
    //    };
    //}   
}