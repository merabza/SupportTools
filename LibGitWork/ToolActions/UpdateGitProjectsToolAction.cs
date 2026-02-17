using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.LibToolActions;

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
        string? workFolder = supportToolsParameters.WorkFolder;
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

        string gitsFolder = Path.Combine(workFolder, "Gits");
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

        foreach (GitProjectsUpdater? gitProjectsUpdater in gitRepos.Gits.OrderBy(k => k.Key).Select(kvp =>
                     GitProjectsUpdater.Create(Logger, _parametersManager, kvp.Key, UseConsole)))
        {
            if (gitProjectsUpdater is null)
            {
                StShared.WriteErrorLine("gitProjectsUpdater does not created", true, Logger);
                return ValueTask.FromResult(false);
            }

            GitProcessor? gitProcessor = gitProjectsUpdater.ProcessOneGitProject();
            if (gitProcessor is null)
            {
                return ValueTask.FromResult(false);
            }

            usedProjectNames.AddRange(gitProjectsUpdater.UsedProjectNames.Except(usedProjectNames));
        }

        foreach (string projectName in supportToolsParameters.GitProjects.Keys.Except(usedProjectNames))
        {
            supportToolsParameters.GitProjects.Remove(projectName);
        }

        Console.WriteLine("Find Dependencies");

        if (!RegisterDependenciesProjects(gitsFolder, supportToolsParameters.GitProjects))
        {
            return ValueTask.FromResult(false);
        }

        _parametersManager.Save(supportToolsParameters, "Project Saved");
        return ValueTask.FromResult(true);
    }

    private static bool RegisterDependenciesProjects(string gitsFolder,
        Dictionary<string, GitProjectDataModel> gitProjects)
    {
        foreach ((string key, GitProjectDataModel project) in gitProjects)
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

            string filePath = Path.Combine(gitsFolder, project.ProjectRelativePath, project.ProjectFileName);
            XElement projectXml = XElement.Load(filePath);

            List<XElement> projectReferences =
                projectXml.Descendants("ItemGroup").Descendants("ProjectReference").ToList();

            foreach (XElement element in projectReferences)
            {
                IEnumerable<XAttribute> attributes = element.Attributes("Include");
                foreach (XAttribute attr in attributes)
                {
                    string? fileFolderName = Path.GetDirectoryName(filePath);
                    if (fileFolderName == null)
                    {
                        return false;
                    }

                    string depProjectFullPath = new DirectoryInfo(Path.Combine(fileFolderName, attr.Value)).FullName;

                    string depProjectRelativePath = Path.GetRelativePath(gitsFolder, depProjectFullPath);

                    string projectName = Path.GetFileNameWithoutExtension(depProjectRelativePath);

                    if (!dependsOnProjectNames.Contains(projectName))
                    {
                        dependsOnProjectNames.Add(projectName);
                    }
                }
            }

            project.DependsOn(dependsOnProjectNames);

            Console.WriteLine();
            return true;
        }

        return true;
    }
}
