using System.Globalization;
using System.IO;
using LibGitWork;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.React;

public sealed class ReCreateReactAppFiles
{
    private readonly ILogger _logger;
    private readonly string _reactAppName;
    private readonly string? _reactTemplateName;
    private readonly string _workFolder;

    public ReCreateReactAppFiles(ILogger logger, string workFolder, string reactAppName, string? reactTemplateName)
    {
        _logger = logger;
        _workFolder = workFolder;
        _reactAppName = reactAppName;
        _reactTemplateName = reactTemplateName;
    }

    public bool Run()
    {
        string reactAppModelsFolderFullName = Path.Combine(_workFolder, "ReactAppModels");

        string? checkedPath = FileStat.CreateFolderIfNotExists(reactAppModelsFolderFullName, true);
        if (checkedPath is null)
        {
            StShared.WriteErrorLine($"does not exists and cannot create work folder {reactAppModelsFolderFullName}",
                true, _logger);
            return false;
        }

        string appName = _reactAppName.ToLower(CultureInfo.CurrentCulture);

        string appFolderFullName = Path.Combine(reactAppModelsFolderFullName, appName);

        if (Directory.Exists(appFolderFullName))
        {
            FileStat.DeleteDirectoryWithNormaliseAttributes(appFolderFullName);
        }

        if (!StShared.RunCmdProcess(
                $"npx create-react-app {appFolderFullName}{(string.IsNullOrWhiteSpace(_reactTemplateName) ? string.Empty : $" --template {_reactTemplateName}")}"))
        {
            StShared.WriteErrorLine("Error When creating react app", true, _logger);
            return false;
        }

        string reactAppModelsForDiffFolderFullName = Path.Combine(_workFolder, "ReactAppModelsForDiff");

        string? checkedDiffPath = FileStat.CreateFolderIfNotExists(reactAppModelsForDiffFolderFullName, true);
        if (checkedDiffPath is null)
        {
            StShared.WriteErrorLine(
                $"does not exists and cannot create work folder {reactAppModelsForDiffFolderFullName}", true, _logger);
            return false;
        }

        string appFolderForDiffFullName = Path.Combine(reactAppModelsForDiffFolderFullName, appName);

        string[] excludes = [".git", "node_modules"];

        if (Directory.Exists(appFolderForDiffFullName))
        {
            FileStat.ClearFolder(appFolderForDiffFullName, excludes);
        }

        FileStat.CopyFilesAndFolders(appFolderFullName, appFolderForDiffFullName, excludes, true, _logger);

        //შემოწმდეს არის თუ არა დაინიცირებული appFolderForDiffFullName ფოლდერში გიტი
        //თუ დაინიცირებულია ვჩერდებით
        //თუ არა უბრალოდ ვაინიცირებთ და ვაკომიტებთ

        var gitProcessor = new GitProcessor(true, _logger, appFolderForDiffFullName);
        if (gitProcessor.IsFolderPartOfGitWorkingTree(appFolderForDiffFullName))
        {
            return true;
        }

        if (gitProcessor.Initialise().IsSome)
        {
            return false;
        }

        return gitProcessor.Add() && gitProcessor.Commit("Initial");
    }
}
