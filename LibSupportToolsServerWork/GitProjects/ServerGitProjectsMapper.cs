using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Paths;
using SupportToolsServerApiContracts.Models;

namespace LibSupportToolsServerWork.GitProjects;

//სერვერის GitProjects-ის (GET git/gitprojects, SupportToolsServer-ის B9) ლოკალურ ფორმაში გადაყვანა, ლოკალური
//„Update Git Projects“-ის შედეგის იდენტურად: გასაღები პროექტის ფაილის სახელია გაფართოების გარეშე, git-ები ლოკალური
//სკანირების რიგით მუშავდება (UpdateGitProjectsToolAction: Gits.OrderBy(k => k.Key)) და განმეორებული სახელისას ბოლოს
//დამუშავებული იმარჯვებს. სერვერი ყველა რეპოზიტორიის ყველა პროექტს აბრუნებს, ამიტომ შეჯახება აქ წყდება და ჩანს.
//ლოკალური Gits-ის გარეთა git-ის პროექტები, როგორც ლოკალურ სკანირებაში, გამოტოვებულია
public static class ServerGitProjectsMapper
{
    public static ServerGitProjects ToLocal(IEnumerable<StsGitProjectDataModel> serverGitProjects,
        IEnumerable<string> localGitNames, PathMapper pathMapper)
    {
        //სერვერის git-ის სახელი ლოკალურ გასაღებს რეგისტრის გაუთვალისწინებლად ემთხვევა (README G8)
        var localGitKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string localGitName in localGitNames)
        {
            localGitKeys.TryAdd(localGitName, localGitName);
        }

        var gitProjectsByGit = new Dictionary<string, List<StsGitProjectDataModel>>();
        List<string> unknownGitNames = [];
        foreach (StsGitProjectDataModel serverGitProject in serverGitProjects)
        {
            if (!localGitKeys.TryGetValue(serverGitProject.GitName, out string? localGitKey))
            {
                if (!unknownGitNames.Contains(serverGitProject.GitName, StringComparer.OrdinalIgnoreCase))
                {
                    unknownGitNames.Add(serverGitProject.GitName);
                }

                continue;
            }

            if (!gitProjectsByGit.TryGetValue(localGitKey, out List<StsGitProjectDataModel>? gitProjectsOfGit))
            {
                gitProjectsOfGit = [];
                gitProjectsByGit.Add(localGitKey, gitProjectsOfGit);
            }

            gitProjectsOfGit.Add(serverGitProject);
        }

        Dictionary<string, GitProjectDataModel> gitProjects = [];
        List<GitProjectDuplicate> duplicates = [];
        foreach ((string gitName, List<StsGitProjectDataModel> gitProjectsOfGit) in
                 gitProjectsByGit.OrderBy(x => x.Key))
        {
            foreach (StsGitProjectDataModel serverGitProject in gitProjectsOfGit)
            {
                string projectName = Path.GetFileNameWithoutExtension(serverGitProject.ProjectFileName);
                if (gitProjects.TryGetValue(projectName, out GitProjectDataModel? replaced))
                {
                    duplicates.Add(new GitProjectDuplicate(projectName, replaced.GitName ?? string.Empty, gitName));
                }

                gitProjects[projectName] = new GitProjectDataModel
                {
                    GitName = gitName,
                    ProjectRelativePath = pathMapper.NormalizeRelativeToLocal(serverGitProject.ProjectRelativePath),
                    ProjectFileName = serverGitProject.ProjectFileName,
                    DependsOnProjectNames = [.. serverGitProject.DependsOnProjectNames]
                };
            }
        }

        return new ServerGitProjects(gitProjects, duplicates, unknownGitNames);
    }

    //რამდენი ჩანაწერი დაემატა, შეიცვალა და წაიშალა. დამოკიდებულებების რიგს მნიშვნელობა არ აქვს: ლოკალურ სიაში ის წინა
    //სკანირებებზეა დამოკიდებული, სერვერისაში კი ანბანითაა
    public static (int Added, int Changed, int Removed) CountChanges(
        IReadOnlyDictionary<string, GitProjectDataModel> oldGitProjects,
        IReadOnlyDictionary<string, GitProjectDataModel> newGitProjects)
    {
        int added = newGitProjects.Keys.Count(x => !oldGitProjects.ContainsKey(x));
        int removed = oldGitProjects.Keys.Count(x => !newGitProjects.ContainsKey(x));
        int changed = newGitProjects.Count(x =>
            oldGitProjects.TryGetValue(x.Key, out GitProjectDataModel? old) && IsChanged(old, x.Value));
        return (added, changed, removed);
    }

    private static bool IsChanged(GitProjectDataModel old, GitProjectDataModel current)
    {
        return old.GitName != current.GitName || old.ProjectRelativePath != current.ProjectRelativePath ||
               old.ProjectFileName != current.ProjectFileName ||
               !old.DependsOnProjectNames.Order(StringComparer.Ordinal)
                   .SequenceEqual(current.DependsOnProjectNames.Order(StringComparer.Ordinal));
    }
}
