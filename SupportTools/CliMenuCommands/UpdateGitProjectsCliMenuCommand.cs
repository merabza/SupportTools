using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CliMenu;
using LibDataInput;
using LibGitWork;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateGitProjectsCliMenuCommand : CliMenuCommand
{
    private readonly string _gitsFolder;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly SupportToolsParameters _supportToolsParameters;
    private readonly string _workFolder;

    private UpdateGitProjectsCliMenuCommand(ILogger logger, IParametersManager parametersManager,
        SupportToolsParameters supportToolsParameters, string workFolder, string gitsFolder) : base(
        "Update Git Projects...")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _workFolder = workFolder;
        _gitsFolder = gitsFolder;
        _supportToolsParameters = supportToolsParameters;
    }

    public static UpdateGitProjectsCliMenuCommand? Create(ILogger logger, IParametersManager parametersManager)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var workFolder = supportToolsParameters.WorkFolder;
        if (string.IsNullOrWhiteSpace(workFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.WorkFolder does not specified", true);
            return null;
        }

        if (!StShared.CreateFolder(workFolder, true))
        {
            StShared.WriteErrorLine(
                $"supportToolsParameters.WorkFolder {workFolder} cannot be created", true);
            return null;
        }

        var gitsFolder = Path.Combine(workFolder, "Gits");
        if (StShared.CreateFolder(gitsFolder, true))
            return new UpdateGitProjectsCliMenuCommand(logger, parametersManager, supportToolsParameters, workFolder,
                gitsFolder);

        StShared.WriteErrorLine($"gitsFolder {gitsFolder} cannot be created", true);
        return null;
    }


    protected override void RunAction()
    {
        try
        {
            //შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
            if (FileStat.CreateFolderIfNotExists(_workFolder, true) == null)
            {
                StShared.WriteErrorLine($"does not exists and cannot create work folder {_workFolder}", true,
                    _logger);
                return;
            }

            //შემოწმდეს ინსტრუმენტების სამუშაო ფოლდერში Gits ფოლდერი თუ არსებობს და თუ არ არსებობს, შეიქმნას
            //_gitsFolder = Path.Combine(_workFolder, "Gits");
            if (FileStat.CreateFolderIfNotExists(_gitsFolder, true) == null)
            {
                StShared.WriteErrorLine($"does not exists and cannot create work folder {_gitsFolder}", true,
                    _logger);
                return;
            }

            List<string> usedProjectNames = [];
            var gitRepos = GitRepos.Create(_logger, _supportToolsParameters.Gits, null, null);

            foreach (var kvp in gitRepos.Gits)
            {
                var gitProjectsUpdater = GitProjectsUpdater.Create(_logger, _parametersManager);
                if (gitProjectsUpdater is null)
                {
                    StShared.WriteErrorLine("gitProjectsUpdater does not created", true, _logger);
                    return;
                }

                if (!gitProjectsUpdater.ProcessOneGitProject(kvp.Key))
                    return;
                usedProjectNames.AddRange(gitProjectsUpdater.UsedProjectNames.Except(usedProjectNames));
            }

            foreach (var projectName in _supportToolsParameters.GitProjects.Keys.Except(usedProjectNames))
                _supportToolsParameters.GitProjects.Remove(projectName);

            Console.WriteLine("Find Dependencies");

            if (!RegisterDependenciesProjects())
                return;

            _parametersManager.Save(_supportToolsParameters, "Project Saved");

            MenuAction = EMenuAction.LevelUp;
            Console.WriteLine("Success");
            return;
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            //StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
        //finally
        //{
        //    StShared.Pause();
        //}

        MenuAction = EMenuAction.Reload;
    }

    private bool RegisterDependenciesProjects()
    {
        foreach (var (key, project) in _supportToolsParameters.GitProjects)
        {
            Console.WriteLine($"Dependencies for {key}");

            if (project.ProjectRelativePath is null)
            {
                StShared.WriteErrorLine("project.ProjectRelativePath is null", true);
                return false;
            }

            List<string> dependsOnProjectNames = [];

            var filePath = Path.Combine(_gitsFolder, project.ProjectRelativePath);
            var projectXml = XElement.Load(filePath);

            var projectReferences =
                projectXml.Descendants("ItemGroup").Descendants("ProjectReference").ToList();

            foreach (var element in projectReferences)
            {
                var attributes = element.Attributes("Include");
                foreach (var attr in attributes)
                {
                    var fileFolderName = Path.GetDirectoryName(filePath);
                    if (fileFolderName == null)
                        return false;
                    var depProjectFullPath =
                        new DirectoryInfo(Path.Combine(fileFolderName, attr.Value)).FullName;

                    var depProjectRelativePath = Path.GetRelativePath(_gitsFolder, depProjectFullPath);

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