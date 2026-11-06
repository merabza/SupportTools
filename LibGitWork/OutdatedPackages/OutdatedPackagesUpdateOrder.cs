using System;
using System.Collections.Generic;
using System.Linq;
using LibGitData.Models;
using LibGitWork.ToolActions;
using SupportToolsData.Models;

namespace LibGitWork.OutdatedPackages;

//git-ების განახლების რიგი. git ახლდება იმ პროექტის ფოლდერში, რომლის სიაშიც ის ერთადერთ დაუმუშავებელ git-ად დარჩა,
//ანუ მას შემდეგ, რაც პროექტის დანარჩენი git-ები (მისი დამოკიდებულებები) უკვე განახლდა. ერთ რაუნდში ყველა ასეთი პროექტი
//სახელის მიხედვით მუშავდება, განახლებული git კი ყველა სიიდან იშლება. რაუნდი ერთი დონეა: მისი git-ები ერთმანეთზე არ
//არის დამოკიდებული (თითოეულის დამოკიდებულებები წინა დონეებშია), ამიტომ ისინი პარალელურად ახლდება.
//ცნობილი არა-C# git-ები (React და სხვა) რიგში არ ითვლება: მათთვის განახლება არაფერს აკეთებს, მათ გამო კი App*
//პროექტები ერთ git-მდე ვერ ჩადიოდნენ და საერთოდ არ ახლდებოდნენ.
//თუ ერთ git-იანი პროექტი აღარ დარჩა (მაგალითად, SupportTools-სა და SupportToolsServer-ს საერთო SupportToolsServerShared
//აქვთ, რომელსაც საკუთარი პროექტი არ აქვს), ჯერ ის git ახლდება, რომელსაც ყველაზე მეტი დარჩენილი პროექტი იყენებს (ის,
//სავარაუდოდ, დანარჩენების დამოკიდებულებაა); თანაბრობისას - ის, რომლის სახელის პროექტიც აღარ დარჩა (პროექტის საკუთარი
//git ბოლოს ახლდება), მერე სახელით. ის ახლდება ამ git-ის მქონე პირველი პროექტის ფოლდერში, ცალკე დონედ
internal static class OutdatedPackagesUpdateOrder
{
    public static List<List<OutdatedPackagesUpdateStep>> Create(
        IEnumerable<KeyValuePair<string, ProjectModel>> projects, IReadOnlyDictionary<string, GitDataModel> gits)
    {
        List<ProjectGits> remaining =
        [
            .. projects.OrderBy(x => x.Key)
                .Select(x => new ProjectGits(x.Key, [.. x.Value.GitProjectNames.Where(y => IsPackageGit(gits, y))]))
                .Where(x => x.GitNames.Count > 0)
        ];
        List<List<OutdatedPackagesUpdateStep>> levels = [];
        while (remaining.Count > 0)
        {
            List<OutdatedPackagesUpdateStep> level = [];
            levels.Add(level);
            List<ProjectGits> singleGitProjects = [.. remaining.Where(x => x.GitNames.Count == 1)];
            if (singleGitProjects.Count == 0)
            {
                string sharedGitName = ChooseSharedGit(remaining);
                //პროექტის სიაში ჩაწერილი სახელით, რადგან GitSyncParameters git-ს ამ სიაში რეგისტრის გათვალისწინებით ეძებს
                (string projectName, string gitName) = remaining
                    .SelectMany(x => x.GitNames.Select(y => (x.ProjectName, GitName: y)))
                    .First(x => string.Equals(x.GitName, sharedGitName, StringComparison.OrdinalIgnoreCase));
                Take(remaining, level, projectName, gitName);
                continue;
            }

            foreach (ProjectGits project in singleGitProjects)
            {
                //იმავე რაუნდის წინა პროექტმა ეს git შეიძლება უკვე განაახლა
                if (project.GitNames.Count == 1)
                {
                    Take(remaining, level, project.ProjectName, project.GitNames[0]);
                }
            }
        }

        return levels;
    }

    //Gits-ში არმყოფი ან ტიპის გარეშე git რიგში რჩება: მისი განახლება ჩავარდება და შეცდომა ბოლოს ჩაიწერება
    private static bool IsPackageGit(IReadOnlyDictionary<string, GitDataModel> gits, string gitName)
    {
        return !gits.TryGetValue(gitName, out GitDataModel? git) ||
               string.IsNullOrWhiteSpace(git.GitIgnorePatternName) ||
               git.GitIgnorePatternName == PackageUpdaterToolAction.CSharp;
    }

    private static string ChooseSharedGit(List<ProjectGits> remaining)
    {
        return remaining.SelectMany(x => x.GitNames).GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => remaining.Exists(y => string.Equals(y.ProjectName, x.Key, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase).First().Key;
    }

    private static void Take(List<ProjectGits> remaining, List<OutdatedPackagesUpdateStep> steps, string projectName,
        string gitName)
    {
        steps.Add(new OutdatedPackagesUpdateStep(projectName, gitName));
        foreach (ProjectGits project in remaining)
        {
            project.GitNames.RemoveAll(x => string.Equals(x, gitName, StringComparison.OrdinalIgnoreCase));
        }

        remaining.RemoveAll(x => x.GitNames.Count == 0);
    }

    private sealed record ProjectGits(string ProjectName, List<string> GitNames);
}
