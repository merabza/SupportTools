using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork;
using LibToolActions;
using LibTools.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibTools.ToolActions;

public sealed class GitClearToolAction : ToolAction
{
    private readonly string? _excludeFolder;
    private readonly GitClearParameters _gitClearParameters;
    private readonly ILogger? _logger;

    public GitClearToolAction(ILogger? logger, GitClearParameters gitClearParameters, string? excludeFolder) : base(
        logger, "Git Clear", null,
        null)
    {
        _logger = logger;
        _gitClearParameters = gitClearParameters;
        _excludeFolder = excludeFolder;
    }

    //public static GitClearToolAction? Create(ILogger logger, ParametersManager parametersManager,
    //    string projectName, EGitCol gitCol, string gitProjectName, bool useConsole)
    //{
    //    var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
    //    var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
    //    var gitClearParameters = GitClearParameters.Create(loggerOrNull,
    //        supportToolsParameters, projectName, gitCol, gitProjectName, useConsole);

    //    if (gitClearParameters is not null)
    //        return new GitClearToolAction(loggerOrNull, gitClearParameters);

    //    StShared.WriteErrorLine("GitClearParameters is not created", true);
    //    return null;
    //}


    protected override bool CheckValidate()
    {
        if (!string.IsNullOrWhiteSpace(_gitClearParameters.GitsFolder))
            return true;
        StShared.WriteErrorLine("Project Folder Name not found.", true);
        return false;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var projectFolderName =
            Path.Combine(_gitClearParameters.GitsFolder, _gitClearParameters.GitData.GitProjectFolderName);
        var gitProcessor = new GitProcessor(true, _logger, projectFolderName);
        if (!Directory.Exists(projectFolderName))
            return ValueTask.FromResult(gitProcessor.Clone(_gitClearParameters.GitData.GitProjectAddress));
        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        var gitInitialized = gitProcessor.IsGitInitialized();

        if (!gitInitialized)
        {
            StShared.WriteErrorLine(
                $"Git project folder exists, but not initialized. folder: {projectFolderName}.", true, _logger);
            return ValueTask.FromResult(false);
        }


        ProcessFolder(projectFolderName);

        return ValueTask.FromResult(true);
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
        var subDirs = dir.GetDirectories().OrderBy(x => x.Name).ToArray();
        var subFiles = dir.GetFiles();
        if (subDirs is [{ Name: "bin" }, { Name: "obj" }] && subFiles.Length == 0 && MustBeDeleted(folderPath))
        {
            Directory.Delete(folderPath, true);
            return true;
        }
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

    private static void DeleteFolderIfExists(string fromFolderPath, string folderNameForDelete)
    {
        var binFolderPath = Path.Combine(fromFolderPath, folderNameForDelete);
        if (Directory.Exists(binFolderPath))
            Directory.Delete(binFolderPath, true);
    }

    private static bool MustBeDeleted(string folderPath)
    {
        if (Directory.GetFiles(folderPath, "*.cs").Length != 0 && Directory.GetFiles(folderPath, "*.cs").Length !=
            Directory.GetFiles(folderPath, "*.AssemblyInfo.cs").Length +
            Directory.GetFiles(folderPath, "*.AssemblyAttributes.cs").Length +
            Directory.GetFiles(folderPath, "*.GlobalUsings.g.cs").Length)
            return false;

        if (Directory.GetDirectories(folderPath, ".git").Length != 0)
            return false;

        var dir = new DirectoryInfo(folderPath);
        return dir.GetDirectories().Where(x => x.Name != ".git").Select(x => x.FullName).All(MustBeDeleted);
    }
}