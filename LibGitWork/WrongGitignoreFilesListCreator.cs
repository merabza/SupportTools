﻿using LibGitData.Models;
using LibGitData;
using SupportToolsData.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibGitWork;

public class WrongGitignoreFilesListCreator
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public WrongGitignoreFilesListCreator(ILogger logger, IParametersManager parametersManager)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }


    public Dictionary<string, string> Create()
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        var gitIgnoreModelFilePaths = supportToolsParameters.GitIgnoreModelFilePaths;

        var projectsList = supportToolsParameters.Projects;
        var gitIgnoreTemplateFileContents = new Dictionary<string, string>();
        var wrongGitIgnoreFilesList = new Dictionary<string, string>();

        var projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();
        foreach (var (projectName, project) in projectsListOrdered)
        {

            foreach (var gitCol in Enum.GetValues<EGitCol>())
            {
                if (!GitStat.CheckGipProject(projectName, project, gitCol, false))
                    continue;

                var gitsFolder = supportToolsParameters.GetGitsFolder(projectName, gitCol);
                if (gitsFolder is null)
                    continue;

                var gitProjects = GitProjects.Create(_logger, supportToolsParameters.GitProjects);

                var gitRepos = GitRepos.Create(_logger, supportToolsParameters.Gits,
                    project.MainProjectFolderRelativePath(gitProjects),
                    project.SpaProjectFolderRelativePath(gitProjects));


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

                    if (!gitIgnoreTemplateFileContents.TryGetValue(gitIgnorePathName,
                            out var gitIgnoreTemplateFileContent))
                    {
                        gitIgnoreTemplateFileContent = File.ReadAllText(gitIgnoreTemplateFileName);
                        gitIgnoreTemplateFileContents.Add(gitIgnorePathName, gitIgnoreTemplateFileContent);
                    }

                    var gitignoreFileName = Path.Combine(gitsFolder, gd.GitProjectFolderName, ".gitignore");
                    var gitignoreFileContent = File.ReadAllText(gitignoreFileName);

                    if (gitignoreFileContent != gitIgnoreTemplateFileContent)
                        wrongGitIgnoreFilesList.Add(gitignoreFileName, gitIgnoreTemplateFileContent);

                }

            }
        }

        return wrongGitIgnoreFilesList;
    }
}