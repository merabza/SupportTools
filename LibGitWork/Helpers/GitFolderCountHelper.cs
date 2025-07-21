using System.IO;
using Microsoft.Extensions.Logging;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared;

namespace LibGitWork.Helpers;

public static class GitFolderCountHelper
{
    public static string? GetProjectFolderName(ILogger? logger, string workFolder, string gitsFolder,
        GitDataDto gitData)
    {
        //შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        if (FileStat.CreateFolderIfNotExists(workFolder, true) == null)
        {
            StShared.WriteErrorLine($"does not exists and cannot create work folder {workFolder}", true, logger);
            return null;
        }

        //შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერში Gits ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        //_gitsFolder = Path.Combine(_workFolder, "Gits");
        if (FileStat.CreateFolderIfNotExists(gitsFolder, true) != null)
            return Path.Combine(gitsFolder,
                gitData.GitProjectFolderName.Replace($"{Path.DirectorySeparatorChar}", "."));

        StShared.WriteErrorLine($"does not exists and cannot create work folder {gitsFolder}", true, logger);
        return null;

        //შემოწმდეს Gits ფოლდერში პროექტის ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
    }
}