using System.IO;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class AppCreatorBaseData
{
    private AppCreatorBaseData(string workPath, string securityPath, string solutionPath, bool forTest)
    {
        WorkPath = workPath;
        SecurityPath = securityPath;
        SolutionPath = solutionPath;
        ForTest = forTest;
    }

    public string WorkPath { get; }
    public string SecurityPath { get; }
    public string SolutionPath { get; }
    public bool ForTest { get; }

    public static AppCreatorBaseData? Create(ILogger logger, AppProjectCreatorData par, bool forTest)
    {
        //შევამოწმოთ და თუ არ არსებობს შევქმნათ სამუშაო ფოლდერი
        if (!StShared.CreateFolder(par.WorkFolderPath, true))
        {
            StShared.WriteErrorLine($"Cannot create work Folder {par.WorkFolderPath}", true, logger);
            return null;
        }

        //შევამოწმოთ არსებობს თუ არა უკვე პროექტის ფოლდერი
        var workPath = Path.Combine(par.WorkFolderPath, par.ProjectName);
        //List<string> foldersForCreate = new List<string> { workPath };

        //დავიანგარიშოთ სოლუშენის ფოლდერის სრული გზა
        var solutionPath = Path.Combine(workPath, par.SolutionFolderName);
        //და გადავინახოთ შესაქმნელი ფოლდერების სიაში
        //foldersForCreate.Add(solutionPath);

        //if (!FileStat.CheckRequiredFolder(true, solutionPath, !forTest))
        //    return null;

        var workSameAsSecurity = FileStat.NormalizePath(par.WorkFolderPath) ==
                                 FileStat.NormalizePath(par.SecurityWorkFolderPath);

        //შევამოწმოთ არსებობს თუ არა უკვე პროექტის ფოლდერი
        var securityPath = Path.Combine(par.SecurityWorkFolderPath,
            $"{par.ProjectName}{(workSameAsSecurity ? ".sec" : "")}");
        //foldersForCreate.Add(securityPath);


        //if (!FileStat.CheckRequiredFolder(true, securityPath, !forTest))
        //    return null;

        return new AppCreatorBaseData(workPath, securityPath, solutionPath, forTest);
    }
}