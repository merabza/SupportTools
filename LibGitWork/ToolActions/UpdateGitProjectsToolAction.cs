using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibGitData.Models;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork.ToolActions;

public sealed class UpdateGitProjectsToolAction : ToolAction
{
    public const string ActionName = "Update Git Projects";
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitProjectsToolAction(ILogger logger, IParametersManager parametersManager, bool useConsole) : base(
        logger, ActionName, null, null, useConsole)
    {
        _parametersManager = parametersManager;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;
        var workFolder = supportToolsParameters.WorkFolder;
        if (string.IsNullOrWhiteSpace(workFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.WorkFolder is not specified", true);
            return ValueTask.FromResult(false);
        }

        if (!StShared.CreateFolder(workFolder, true))
        {
            StShared.WriteErrorLine($"supportToolsParameters.WorkFolder {workFolder} cannot be created", true);
            return ValueTask.FromResult(false);
        }

        var gitsFolder = Path.Combine(workFolder, "Gits");
        //შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერში Gits ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
        //_gitsFolder = Path.Combine(_workFolder, "Gits");
        if (!StShared.CreateFolder(gitsFolder, true))
        {
            StShared.WriteErrorLine($"gitsFolder folder {gitsFolder} is not exists and cannot be created", true,
                Logger);
            return ValueTask.FromResult(false);
        }

        List<string> usedProjectNames = [];
        var gitRepos = GitRepos.Create(Logger, supportToolsParameters.Gits, null, UseConsole, true);

        foreach (var gitProjectsUpdater in gitRepos.Gits.Select(kvp =>
                     GitProjectsUpdater.Create(Logger, _parametersManager, kvp.Key, UseConsole)))
        {
            if (gitProjectsUpdater is null)
            {
                StShared.WriteErrorLine("gitProjectsUpdater does not created", true, Logger);
                return ValueTask.FromResult(false);
            }

            var gitProcessor = gitProjectsUpdater.ProcessOneGitProject();
            if (gitProcessor is null)
                return ValueTask.FromResult(false);
            usedProjectNames.AddRange(gitProjectsUpdater.UsedProjectNames.Except(usedProjectNames));
        }

        foreach (var projectName in supportToolsParameters.GitProjects.Keys.Except(usedProjectNames))
            supportToolsParameters.GitProjects.Remove(projectName);

        Console.WriteLine("Find Dependencies");

        if (!RegisterDependenciesProjects(gitsFolder, supportToolsParameters.GitProjects))
            return ValueTask.FromResult(false);

        _parametersManager.Save(supportToolsParameters, "Project Saved");
        return ValueTask.FromResult(true);
    }

    private static bool RegisterDependenciesProjects(string gitsFolder,
        Dictionary<string, GitProjectDataModel> gitProjects)
    {
        foreach (var (key, project) in gitProjects)
        {
            Console.WriteLine($"Dependencies for {key}");

            if (project.ProjectRelativePath is null)
            {
                StShared.WriteErrorLine("project.ProjectRelativePath is null", true);
                return false;
            }

            if (project.ProjectFileName is null)
            {
                StShared.WriteErrorLine("project.ProjectFileName is null", true);
                return false;
            }

            List<string> dependsOnProjectNames = [];

            var filePath = Path.Combine(gitsFolder, project.ProjectRelativePath, project.ProjectFileName);
            var projectXml = XElement.Load(filePath);

            var projectReferences = projectXml.Descendants("ItemGroup").Descendants("ProjectReference").ToList();

            foreach (var element in projectReferences)
            {
                var attributes = element.Attributes("Include");
                foreach (var attr in attributes)
                {
                    var fileFolderName = Path.GetDirectoryName(filePath);
                    if (fileFolderName == null)
                        return false;
                    var depProjectFullPath = new DirectoryInfo(Path.Combine(fileFolderName, attr.Value)).FullName;

                    var depProjectRelativePath = Path.GetRelativePath(gitsFolder, depProjectFullPath);

                    var projectName = Path.GetFileNameWithoutExtension(depProjectRelativePath);

                    if (!dependsOnProjectNames.Contains(projectName))
                        dependsOnProjectNames.Add(projectName);
                }
            }

            project.DependsOn(dependsOnProjectNames);


            Console.WriteLine();
            return true;
        }

        return true;
    }
}