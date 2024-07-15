using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibParameters;
using LibToolActions;
using LibTools.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibTools.ToolActions;

public class ClearMultipleProjectsToolAction : ToolAction
{
    //private readonly ParametersManager _parametersManager;
    private readonly ClearAllProjectsParameters _clearAllProjectsParameters;
    private readonly string? _excludeFolder;
    private readonly ILogger? _logger;

    private ClearMultipleProjectsToolAction(ILogger logger, ClearAllProjectsParameters clearAllProjectsParameters,
        string? excludeFolder, bool useConsole) : base(logger, "Clear Multiple Projects", null, null, useConsole)
    {
        _logger = logger;
        //_parametersManager = parametersManager;
        _clearAllProjectsParameters = clearAllProjectsParameters;
        _excludeFolder = excludeFolder;
    }


    public static ClearMultipleProjectsToolAction Create(ILogger logger, ParametersManager parametersManager,
        string? projectGroupName, string? projectName, bool useConsole)
    {
        //D:\1WorkDotnet\SupportTools\SupportTools\SupportTools\bin\Debug\net8.0
        var baseFolder = AppContext.BaseDirectory;
        string? excludeFolder = null;
        var baseDir = new DirectoryInfo(baseFolder);
        if (baseDir.Parent is { Name: "Debug", Parent.Name: "bin" })
            excludeFolder = baseDir.Parent.Parent.Parent?.FullName;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var clearAllProjectsParameters =
            ClearAllProjectsParameters.Create(supportToolsParameters, projectGroupName, projectName);
        return new ClearMultipleProjectsToolAction(logger, clearAllProjectsParameters, excludeFolder, useConsole);
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList;
        if (_clearAllProjectsParameters.ProjectGroupName is null &&
            _clearAllProjectsParameters.ProjectName is null)
            projectsList = _clearAllProjectsParameters.Projects;
        else if (_clearAllProjectsParameters.ProjectGroupName is not null)
            projectsList = _clearAllProjectsParameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _clearAllProjectsParameters.ProjectGroupName);
        else
            projectsList =
                _clearAllProjectsParameters.Projects.Where(x =>
                    x.Key == _clearAllProjectsParameters.ProjectName);

        var projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        //var changedGitProjects = new Dictionary<EGitCollect, Dictionary<string, List<string>>>
        //{
        //    [EGitCollect.Collect] = [],
        //    [EGitCollect.Usage] = []
        //};
        //var loopNom = 0;
        //var gitCollectUsage = EGitCollect.Collect;
        //while (gitCollectUsage == EGitCollect.Collect || changedGitProjects[EGitCollect.Collect].Count > 0)
        //{
        //changedGitProjects[EGitCollect.Collect] = [];
        //Console.WriteLine($"---=== {gitCollectUsage} {(loopNom == 0 ? string.Empty : loopNom)} ===---");
        //პროექტების ჩამონათვალი
        foreach (var (projectName, project) in projectsListOrdered)
        {
            ClearOneSolution(projectName, project, EGitCol.Main);
            if (_clearAllProjectsParameters.ScaffoldSeedersWorkFolder is not null)
                ClearOneSolution(projectName, project, EGitCol.ScaffoldSeed);
        }

        //Console.WriteLine("---===---------===---");

        //gitCollectUsage = EGitCollect.Usage;
        //loopNom++;
        //changedGitProjects[EGitCollect.Usage] = changedGitProjects[EGitCollect.Collect];
        //}

        return Task.FromResult(true);
    }

    private void ClearOneSolution(string projectName, ProjectModel project, EGitCol gitCol)
    {
        //var clearOneSolutionAllProjectsToolAction = ClearOneSolutionAllProjectsToolAction.Create(_logger, _parametersManager,
        //    projectName);
        //clearOneSolutionAllProjectsToolAction.Run(CancellationToken.None).Wait();

        if (project.ProjectFolderName is null)
        {
            StShared.WriteErrorLine($"ProjectFolderName is not specified for project {projectName}", true, _logger,
                false);
            return;
        }

        if (gitCol == EGitCol.Main)
            ProcessFolder(project.ProjectFolderName);

        //todo EGitCol.ScaffoldSeed
    }

    private static bool AreFolderNamesEqual(string folderPath1, string folderPath2)
    {
        var normalizedPath1 = Path.GetFullPath(folderPath1).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedPath2 = Path.GetFullPath(folderPath2).TrimEnd(Path.DirectorySeparatorChar);

        //var dir1 = new DirectoryInfo(normalizedPath1);
        //var dir2 = new DirectoryInfo(normalizedPath2);


        //if (dir1.Root.Name != dir2.Root.Name &&
        //    dir1.Root.Name.Equals(dir2.Root.Name, StringComparison.CurrentCultureIgnoreCase))
        //{
        //    string relativePath1 = Path.GetRelativePath(dir1.Root.Name, dir1.FullName);
        //    string relativePath2 = Path.GetRelativePath(dir2.Root.Name, dir2.FullName);
        //    normalizedPath1 = Path.Combine(dir1.Root.Name, relativePath1);
        //    normalizedPath2 = Path.Combine(dir1.Root.Name, relativePath2);
        //}

        if (SystemStat.IsWindows())
            return string.Equals(normalizedPath1, normalizedPath2, StringComparison.OrdinalIgnoreCase);
        return normalizedPath1 == normalizedPath2;
    }

    private bool ProcessFolder(string folderPath)
    {
        Console.WriteLine($"Process Folder {folderPath}");

        if (_excludeFolder is not null && AreFolderNamesEqual(folderPath, _excludeFolder))
            return true;

        if (folderPath == _excludeFolder)
            return true;

        if (Directory.GetFiles(folderPath, "*.esproj").Length != 0)
            return true;

        var dir = new DirectoryInfo(folderPath);
        //if (_excludeFolder is not null)
        //{
        //    var excludeFolder = new DirectoryInfo(_excludeFolder);
        //    if (dir.FullName == excludeFolder.FullName)
        //        return true;
        //}

        if (dir.Name is "bin" or "obj")
        {
            StShared.WriteWarningLine($"may be folder will be deleted {folderPath}", true, _logger);
            return true;
        }

        if (Directory.GetFiles(folderPath, "*.csproj").Length == 0)
            return dir.GetDirectories().Where(x => x.Name != ".git" && x.Name != ".vs" && x.Name != "packages")
                .Select(x => x.FullName)
                .All(ProcessFolder);

        if (folderPath.Contains(".git"))
        {
            StShared.WriteErrorLine($"Folder Path {folderPath} contains .git", true, _logger, false);
            return false;
        }

        //bin
        DeleteFolderIfExists(folderPath, "bin");
        //obj
        DeleteFolderIfExists(folderPath, "obj");

        if (MustBeDeleted(folderPath))
            Directory.Delete(folderPath, true);

        return true;
    }

    private static bool MustBeDeleted(string folderPath)
    {
        if (Directory.GetFiles(folderPath, "*.cs").Length != 0)
            return false;

        if (Directory.GetDirectories(folderPath, ".git").Length != 0)
            return false;

        var dir = new DirectoryInfo(folderPath);
        return dir.GetDirectories().Where(x => x.Name != ".git").Select(x => x.FullName).All(MustBeDeleted);
    }

    private static void DeleteFolderIfExists(string fromFolderPath, string folderNameForDelete)
    {
        var binFolderPath = Path.Combine(fromFolderPath, folderNameForDelete);
        if (Directory.Exists(binFolderPath))
            Directory.Delete(binFolderPath, true);
    }
}