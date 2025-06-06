using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class AppCreatorBaseData
{
    private AppCreatorBaseData(string workPath, string securityPath, string solutionPath,
        Dictionary<string, string> gitIgnoreModelFilePaths)
    {
        WorkPath = workPath;
        SecurityPath = securityPath;
        SolutionPath = solutionPath;
        GitIgnoreModelFilePaths = gitIgnoreModelFilePaths;
    }

    public string WorkPath { get; }
    public string SecurityPath { get; }
    public string SolutionPath { get; }
    public Dictionary<string, string> GitIgnoreModelFilePaths { get; }

    public static AppCreatorBaseData? Create(ILogger logger, string workFolderPath, string projectName,
        string solutionFolderName, string securityWorkFolderPath,
        Dictionary<string, string> gitIgnoreModelFilePaths)
    {
        //შევამოწმოთ და თუ არ არსებობს შევქმნათ სამუშაო ფოლდერი
        if (!StShared.CreateFolder(workFolderPath, true))
        {
            StShared.WriteErrorLine($"Cannot create work Folder {workFolderPath}", true, logger);
            return null;
        }

        //პროექტის ფოლდერი
        var workPath = Path.Combine(workFolderPath, projectName);

        //შევამოწმოთ და თუ არ არსებობს შევქმნათ სამუშაო ფოლდერი
        if (!StShared.CreateFolder(securityWorkFolderPath, true))
        {
            StShared.WriteErrorLine($"Cannot create security Folder {securityWorkFolderPath}", true, logger);
            return null;
        }

        //პროექტის ფოლდერი
        var securityPath = Path.Combine(securityWorkFolderPath, projectName);

        //დავიანგარიშოთ სოლუშენის ფოლდერის სრული გზა
        var solutionPath = Path.Combine(workPath, solutionFolderName);

        return new AppCreatorBaseData(workPath, securityPath, solutionPath, gitIgnoreModelFilePaths);
    }
}