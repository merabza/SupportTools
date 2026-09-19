using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class AppCreatorBaseData
{
    private AppCreatorBaseData(string workPath, string securityPath, string solutionPath,
        string? folderForGitignoreFiles, List<string> gitIgnoreModels)
    {
        WorkPath = workPath;
        SecurityPath = securityPath;
        SolutionPath = solutionPath;
        FolderForGitignoreFiles = folderForGitignoreFiles;
       GitIgnorePatterns = gitIgnoreModels;
    }

    public string WorkPath { get; }
    public string SecurityPath { get; }
    public string SolutionPath { get; }
    public string? FolderForGitignoreFiles { get; }
    public List<string>GitIgnorePatterns { get; }

    public static AppCreatorBaseData? Create(ILogger logger, string workFolderPath, string projectName,
        string solutionFolderName, string securityWorkFolderPath, string? folderForGitignoreFiles,
        List<string> gitIgnoreModels)
    {
        //შევამოწმოთ და თუ არ არსებობს შევქმნათ სამუშაო ფოლდერი
        if (!StShared.CreateFolder(workFolderPath, true))
        {
            StShared.WriteErrorLine($"Cannot create work Folder {workFolderPath}", true, logger);
            return null;
        }

        //პროექტის ფოლდერი
        string workPath = Path.Combine(workFolderPath, projectName);

        //შევამოწმოთ და თუ არ არსებობს შევქმნათ სამუშაო ფოლდერი
        if (!StShared.CreateFolder(securityWorkFolderPath, true))
        {
            StShared.WriteErrorLine($"Cannot create security Folder {securityWorkFolderPath}", true, logger);
            return null;
        }

        //პროექტის ფოლდერი
        string securityPath = Path.Combine(securityWorkFolderPath, projectName);

        //დავიანგარიშოთ სოლუშენის ფოლდერის სრული გზა
        string solutionPath = Path.Combine(workPath, solutionFolderName);

        return new AppCreatorBaseData(workPath, securityPath, solutionPath, folderForGitignoreFiles,
            gitIgnoreModels);
    }
}
