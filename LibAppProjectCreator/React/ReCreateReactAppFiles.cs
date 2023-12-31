using System;
using System.IO;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
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
        var reactAppModelsFolderFullName = Path.Combine(_workFolder, "ReactAppModels");

        var checkedPath = FileStat.CreateFolderIfNotExists(reactAppModelsFolderFullName, true);
        if (checkedPath is null)
        {
            StShared.WriteErrorLine($"does not exists and cannot create work folder {reactAppModelsFolderFullName}",
                true, _logger);
            return false;
        }

        var appName = _reactAppName.ToLower();

        var appFolderFullName = Path.Combine(reactAppModelsFolderFullName, appName);

        if (Directory.Exists(appFolderFullName))
            FileStat.DeleteDirectory(appFolderFullName);

        if (!StShared.RunCmdProcess(
                $"npx create-react-app {appFolderFullName}{(string.IsNullOrWhiteSpace(_reactTemplateName) ? "" : $" --template {_reactTemplateName}")}"))
        {
            StShared.WriteErrorLine("Error When creating react app", true, _logger);
            return false;
        }

        var reactAppModelsForDiffFolderFullName = Path.Combine(_workFolder, "ReactAppModelsForDiff");

        var checkedDiffPath = FileStat.CreateFolderIfNotExists(reactAppModelsForDiffFolderFullName, true);
        if (checkedDiffPath is null)
        {
            StShared.WriteErrorLine(
                $"does not exists and cannot create work folder {reactAppModelsForDiffFolderFullName}", true, _logger);
            return false;
        }

        var appFolderForDiffFullName = Path.Combine(reactAppModelsForDiffFolderFullName, appName);

        string[] excludes = { ".git", "node_modules" };

        if (Directory.Exists(appFolderForDiffFullName))
            FileStat.ClearFolder(appFolderForDiffFullName, excludes);

        FileStat.CopyFilesAndFolders(appFolderFullName, appFolderForDiffFullName, excludes, true, _logger);

        //შემოწმდეს არის თუ არა დაინიცირებული appFolderForDiffFullName ფოლდერში გიტი
        //თუ დაინიცირებულია ვჩერდებით
        //თუ არა უბრალოდ ვაინიცირებთ და ვაკომიტებთ

        var isInsideWorkTreeResult = StShared.RunProcessWithOutput(true, _logger, "git",
            $"-C \"{appFolderForDiffFullName}\" rev-parse --is-inside-work-tree", new[] {128});
        if (isInsideWorkTreeResult.IsT1)
            return false;

        var isInsideWorkTree = isInsideWorkTreeResult.AsT0;

        if ( isInsideWorkTree.Item2 == 0 && isInsideWorkTree.Item1 == "true" + Environment.NewLine)
            return true;

        if (StShared.RunProcess(true, _logger, "git", $"-C \"{appFolderForDiffFullName}\" init").IsSome)
            return false;

        return StShared.RunProcess(true, _logger, "git", $"-C \"{appFolderForDiffFullName}\" add .").IsNone && StShared
            .RunProcess(true, _logger, "git", $"-C \"{appFolderForDiffFullName}\" commit -m \"Initial\"").IsNone;
    }
}