using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGitData;
using LibGitData.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork;

public class WrongGitignoreFilesListCreator
{
    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public WrongGitignoreFilesListCreator(ILogger? logger, IParametersManager parametersManager, bool useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _useConsole = useConsole;
    }


    public Dictionary<string, string> Create()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        var gitIgnoreModelFilePaths = supportToolsParameters.GitIgnoreModelFilePaths;

        var projectsList = supportToolsParameters.Projects;
        var gitIgnoreTemplateFileContents = new Dictionary<string, string>();
        var missingGitIgnoreTemplateFiles = new Dictionary<string, string>();
        var wrongGitIgnoreFilesList = new Dictionary<string, string>();

        var projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();
        foreach (var (projectName, project) in projectsListOrdered)
        foreach (var gitCol in Enum.GetValues<EGitCol>())
        {
            if (!GitStat.CheckGitProject(projectName, project, gitCol, false))
                continue;

            var gitsFolder = supportToolsParameters.GetGitsFolder(projectName, gitCol);
            if (gitsFolder is null)
                continue;

            var gitProjects = GitProjects.Create(_logger, supportToolsParameters.GitProjects);

            var gitRepos = GitRepos.Create(_logger, supportToolsParameters.Gits,
                project.SpaProjectFolderRelativePath(gitProjects), _useConsole, false);


            var gitProjectNames = gitCol switch
            {
                EGitCol.Main => project.GitProjectNames,
                EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
                _ => null
            } ?? [];

            var gitData = gitRepos.Gits.Where(x => gitProjectNames.Contains(x.Key)).Select(x => x.Value);
            foreach (var gd in gitData.OrderBy(x => x.GitProjectFolderName))
            {
                var gitIgnorePathName = gd.GitIgnorePathName;
                if (!gitIgnoreModelFilePaths.TryGetValue(gitIgnorePathName, out var gitIgnoreTemplateFileName))
                    continue;

                if (missingGitIgnoreTemplateFiles.ContainsKey(gitIgnorePathName))
                    continue;

                if (!gitIgnoreTemplateFileContents.TryGetValue(gitIgnorePathName, out var gitIgnoreTemplateFileContent))
                {
                    if (File.Exists(gitIgnoreTemplateFileName))
                    {
                        gitIgnoreTemplateFileContent = File.ReadAllText(gitIgnoreTemplateFileName);
                    }
                    else
                    {
                        missingGitIgnoreTemplateFiles.Add(gitIgnorePathName, gitIgnoreTemplateFileName);
                        StShared.WriteErrorLine($"{gitIgnoreTemplateFileName} is not exists", true, _logger, false);
                        continue;
                    }

                    gitIgnoreTemplateFileContents.Add(gitIgnorePathName, gitIgnoreTemplateFileContent);
                }

                var gitignoreFileName = Path.Combine(gitsFolder, gd.GitProjectFolderName, ".gitignore");

                if (File.Exists(gitignoreFileName))
                {
                    var gitignoreFileContent = File.ReadAllText(gitignoreFileName);
                    if (gitignoreFileContent != gitIgnoreTemplateFileContent)
                        wrongGitIgnoreFilesList.TryAdd(gitignoreFileName, gitIgnoreTemplateFileContent);
                }
                else
                {
                    wrongGitIgnoreFilesList.TryAdd(gitignoreFileName, gitIgnoreTemplateFileContent);
                }
            }
        }

        return wrongGitIgnoreFilesList;
    }
}