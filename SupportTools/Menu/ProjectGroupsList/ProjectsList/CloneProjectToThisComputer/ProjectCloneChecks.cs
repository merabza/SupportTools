using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGitData;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.Helpers;
using LibGitWork.Models;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//„Clone Project To This Computer“-ის შემოწმებები (D3). git-ს არ უშვებს, მხოლოდ პარამეტრებსა და ფაილებს კითხულობს
internal static class ProjectCloneChecks
{
    //პროექტის git-ები, რომელთა csproj/esproj ჩანაწერი GitProjects-ში არ არის (Update Git Projects ამ git-ზე არ გაშვებულა).
    //Gits-ში არარსებული სახელი აქ არ ითვლება: მას სინქრონიზაციაც ვერ დაკლონავს
    public static List<string> GetGitsWithoutGitProjects(SupportToolsParameters parameters,
        IEnumerable<string> gitNames)
    {
        HashSet<string> gitsWithProjects =
            new(parameters.GitProjects.Values.Select(x => x.GitName).OfType<string>(),
                StringComparer.OrdinalIgnoreCase);
        return
        [
            .. gitNames.Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => parameters.Gits.ContainsKey(x) && !gitsWithProjects.Contains(x))
        ];
    }

    //git-ების ლოკალური ფოლდერები, ისევე როგორც სინქრონიზაციისას (GitSyncParameters). null - ფოლდერი ვერ დგინდება
    //(git Gits-ში არ არის, ან {SpaProjectFolderRelativePath}-ს SpaProjectName-ის GitProjects ჩანაწერი აკლია)
    public static Dictionary<string, string?> GetGitFolders(SupportToolsParameters parameters, string projectName,
        ProjectModel project, string? gitsFolder, IEnumerable<string> gitNames)
    {
        var gitProjects = GitProjects.Create(null, parameters.GitProjects);
        (string? spaProjectFolderRelativePath, _) = SpaProjectFolderHelper.GetSpaProjectFolderRelativePath(null,
            projectName, project, gitProjects, parameters.Gits, [], false);
        var gitRepos = GitRepos.Create(null, parameters.Gits, spaProjectFolderRelativePath, false, false);

        Dictionary<string, string?> gitFolders = new(StringComparer.OrdinalIgnoreCase);
        foreach (string gitName in gitNames)
        {
            gitFolders[gitName] = gitsFolder is not null && gitRepos.Gits.TryGetValue(gitName, out GitData? gitData)
                ? Path.Combine(gitsFolder, gitData.GitProjectFolderName)
                : null;
        }

        return gitFolders;
    }

    //შაბლონების ფაილები: git-ების .gitignore და პროექტის .editorconfig შაბლონები ამ კომპიუტერზე არის და კლონის ფაილები
    //მათ ემთხვევა. ბრუნდება მხოლოდ პრობლემები
    public static List<string> GetTemplateProblems(SupportToolsParameters parameters, ProjectModel project,
        IReadOnlyDictionary<string, string?> gitFolders)
    {
        List<string> problems = [];
        AddGitIgnoreProblems(parameters, gitFolders, problems);
        AddEditorConfigProblems(parameters, project, problems);
        return problems;
    }

    //პროექტის საიდუმლო ფაილები (C6-ის ველები), რომლებიც ამ კომპიუტერზე არ არის
    public static List<string> GetMissingStoredFiles(ProjectModel project)
    {
        IEnumerable<string?> paths =
        [
            project.SeedProjectParametersFilePath,
            project.PrepareProdCopyDatabaseProjectParametersFilePath,
            project.PairedDbObjectsResultFileName,
            .. project.ServerInfos.Values.Select(x => x.AppSettingsJsonSourceFileName)
        ];
        return
        [
            .. paths.OfType<string>().Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase).Where(x => !File.Exists(x))
        ];
    }

    private static void AddGitIgnoreProblems(SupportToolsParameters parameters,
        IReadOnlyDictionary<string, string?> gitFolders, List<string> problems)
    {
        foreach ((string gitName, string? gitFolder) in gitFolders.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!parameters.Gits.TryGetValue(gitName, out GitDataModel? git) ||
                string.IsNullOrWhiteSpace(git.GitIgnorePatternName))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(parameters.FolderForGitignoreFiles))
            {
                problems.Add("FolderForGitignoreFiles is not specified, the .gitignore files are not checked");
                return;
            }

            string templateFileName = SupportToolsParameters.GetGitIgnoreModelFilePath(
                parameters.FolderForGitignoreFiles, git.GitIgnorePatternName);
            //დაუკლონავი git-ის (ფოლდერი null) მხოლოდ შაბლონი მოწმდება
            AddFileProblem(gitFolder is null ? null : Path.Combine(gitFolder, ".gitignore"), templateFileName,
                $".gitignore template {git.GitIgnorePatternName}", problems);
        }
    }

    private static void AddEditorConfigProblems(SupportToolsParameters parameters, ProjectModel project,
        List<string> problems)
    {
        string? editorConfigFileName = project.EditorConfigFileName();
        if (string.IsNullOrWhiteSpace(project.EditorConfigPatternName) || editorConfigFileName is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(parameters.FolderForEditorConfigFiles))
        {
            problems.Add("FolderForEditorConfigFiles is not specified, the .editorconfig file is not checked");
            return;
        }

        string templateFileName = SupportToolsParameters.GetEditorConfigPatternFilePath(
            parameters.FolderForEditorConfigFiles, project.EditorConfigPatternName);
        AddFileProblem(editorConfigFileName, templateFileName,
            $".editorconfig template {project.EditorConfigPatternName}", problems);
    }

    //შაბლონი ჯერ ამ კომპიუტერზე უნდა იყოს (Sync Registry მას შაბლონების ფოლდერში წერს), მერე ფაილი მას უნდა ემთხვეოდეს
    private static void AddFileProblem(string? fileName, string templateFileName, string templateDescription,
        List<string> problems)
    {
        if (!File.Exists(templateFileName))
        {
            problems.Add($"The {templateDescription} is missing: {templateFileName}");
            return;
        }

        //ფაილის ადგილი არ დგინდება, შესადარებელი არაფერია
        if (fileName is null)
        {
            return;
        }

        if (!File.Exists(fileName))
        {
            problems.Add($"{fileName} does not exist ({templateDescription})");
        }
        else if (File.ReadAllText(fileName) != File.ReadAllText(templateFileName))
        {
            problems.Add($"{fileName} differs from the {templateDescription}");
        }
    }
}
