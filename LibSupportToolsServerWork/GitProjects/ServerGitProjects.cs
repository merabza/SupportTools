using System.Collections.Generic;
using LibGitData.Models;

namespace LibSupportToolsServerWork.GitProjects;

//სერვერის GitProjects ლოკალური ფორმით: პროექტები პროექტის სახელის გასაღებით, სახელის შეჯახებები (ორ რეპოზიტორიაში
//ერთნაირი სახელის პროექტი) და სერვერის ის git-ები, რომლებიც ლოკალურ Gits-ში არ არის
public sealed record ServerGitProjects(
    Dictionary<string, GitProjectDataModel> GitProjects,
    List<GitProjectDuplicate> Duplicates,
    List<string> UnknownGitNames);

//პროექტის სახელი ორ რეპოზიტორიაშია: ReplacedGitName-ის პროექტი GitName-ის პროექტმა ჩაანაცვლა
public sealed record GitProjectDuplicate(string ProjectName, string ReplacedGitName, string GitName);
