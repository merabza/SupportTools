using System.Collections.Generic;
using System.IO;
using LibGitData;
using LibGitData.Models;
using LibGitWork.ToolCommandParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;
using SupportToolsData.Models;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//git-ის გარეშე: სკანირება GitProjects-ში წინასწარ მომზადებულ ჩანაწერებს ამატებს, კლონი კი ქმნის იმ ფოლდერს, რომელსაც
//რეალური სინქრონიზაცია ითვლის (GitSyncParameters), და შიგ .gitignore-ს წერს
internal sealed class FakeProjectCloneGitActions : IProjectCloneGitActions
{
    private readonly SupportToolsParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FakeProjectCloneGitActions(SupportToolsParameters parameters)
    {
        _parameters = parameters;
    }

    public List<string> Calls { get; } = [];

    //git-ის სახელი => სკანირების შედეგად დასამატებელი ჩანაწერები (GitProjects-ის გასაღები => ჩანაწერი)
    public Dictionary<string, Dictionary<string, GitProjectDataModel>> ScanResults { get; } = [];

    public HashSet<string> FailingScans { get; } = [];
    public HashSet<string> FailingClones { get; } = [];
    public string GitIgnoreContent { get; set; } = "bin/";

    public bool ScanGitProjects(string gitName)
    {
        Calls.Add($"scan {gitName}");
        if (FailingScans.Contains(gitName))
        {
            return false;
        }

        foreach ((string key, GitProjectDataModel gitProject) in ScanResults.GetValueOrDefault(gitName) ?? [])
        {
            _parameters.GitProjects[key] = gitProject;
        }

        return true;
    }

    public void SyncGit(string projectName, EGitCol gitCol, string gitName)
    {
        Calls.Add($"sync {gitCol} {gitName}");
        if (FailingClones.Contains(gitName))
        {
            return;
        }

        var gitSyncParameters = GitSyncParameters.Create(null, _parameters, projectName, gitCol, gitName, false);
        if (gitSyncParameters is null)
        {
            return;
        }

        string gitFolder = Path.Combine(gitSyncParameters.GitsFolder, gitSyncParameters.GitData.GitProjectFolderName);
        Directory.CreateDirectory(gitFolder);
        File.WriteAllText(Path.Combine(gitFolder, ".gitignore"), GitIgnoreContent);
    }
}
