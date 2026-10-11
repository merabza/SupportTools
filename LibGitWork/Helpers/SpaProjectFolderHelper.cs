using System;
using System.Collections.Generic;
using System.Linq;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibGitWork.Helpers;

//SpaProjectName-ის მქონე პროექტის {SpaProjectFolderRelativePath}-ით დაწყებული git-ის ფოლდერი SpaProjectName-ის GitProjects
//ჩანაწერით დგინდება. ჩანაწერის გარეშე GitProjects.GetGitProjectByKey exception-ს ისვრის და V2 სინქრონიზაცია ჩერდება (D3),
//ამიტომ ეს დამხმარე exception-ის ნაცვლად ნათელ შეცდომას წერს
public static class SpaProjectFolderHelper
{
    public static string GetMissingSpaProjectMessage(string projectName, string? spaProjectName)
    {
        return
            $"GitProjects has no {spaProjectName} (SpaProjectName of project {projectName}): run \"Update Git Projects\" or \"Update Git Projects From SupportToolsServer\"";
    }

    //ბრუნდება GitRepos.Create-ის spaProjectFolderRelativePath. თუ ჩანაწერი აკლია, Path არის null (GitRepos.Create ასეთ
    //git-ებს ტოვებს), ხოლო თუ gitNames-იდან რომელიმეს ეს ფოლდერი სჭირდება, IsMissing=true და შეცდომა იწერება
    public static (string? Path, bool IsMissing) GetSpaProjectFolderRelativePath(ILogger? logger, string projectName,
        ProjectModel project, GitProjects gitProjects, Dictionary<string, GitDataModel> gits,
        IEnumerable<string> gitNames, bool useConsole)
    {
        if (!project.IsSpaProjectMissing(gitProjects))
        {
            return (project.SpaProjectFolderRelativePath(gitProjects), false);
        }

        bool isMissing = gitNames.Any(gitName =>
            gits.TryGetValue(gitName, out GitDataModel? git) && UsesSpaProjectFolder(git));
        if (isMissing)
        {
            StShared.WriteErrorLine(GetMissingSpaProjectMessage(projectName, project.SpaProjectName), useConsole,
                logger, false);
        }

        return (null, isMissing);
    }

    public static bool UsesSpaProjectFolder(GitDataModel git)
    {
        return git.GitProjectFolderName is not null &&
               git.GitProjectFolderName.StartsWith(GitDataModel.SpaProjectFolderRelativePathName,
                   StringComparison.Ordinal);
    }
}
