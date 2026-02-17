using System.Collections.Generic;
using System.IO;
using ConnectionTools.ConnectTools;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.FolderProcessors;

public sealed class CopyAndReplaceFilesAndFolders : FolderProcessor
{
    //private readonly Dictionary<string, List<MyFileInfo>> _checkedFolderFiles = new();
    private readonly List<string> _checkedFolders = [];

    private readonly FileManager _destinationFileManager;

    public CopyAndReplaceFilesAndFolders(FileManager sourceFileManager, FileManager destinationFileManager,
        ExcludeSet excludeSet) : base("Copy And Replace files and folders",
        "First part of files synchronization Copy And Replace files from one place to another", sourceFileManager, null,
        false, excludeSet, true, true)
    {
        _destinationFileManager = destinationFileManager;
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        List<string> dirNames = afterRootPath is null
            ? []
            : afterRootPath.PrepareAfterRootPath(FileManager.DirectorySeparatorChar);
        string? preparedDestinationAfterRootPath = CheckDestinationDirs(dirNames);

        string sourceFileFullPath = FileManager.GetPath(afterRootPath, file.FileName);
        string destinationFileFullPath =
            _destinationFileManager.GetPath(preparedDestinationAfterRootPath, file.FileName);

        if (File.Exists(destinationFileFullPath))
        {
            if (FileStat.FileCompare(sourceFileFullPath, destinationFileFullPath))
            {
                return true;
            }

            File.Delete(destinationFileFullPath);
        }

        File.Copy(sourceFileFullPath, destinationFileFullPath);
        return true;
    }

    private string? CheckDestinationDirs(IEnumerable<string> dirNames)
    {
        string? afterRootPath = null;
        foreach (string dir in dirNames)
        {
            string forCreateDirPart = _destinationFileManager.PathCombine(afterRootPath, dir);
            if (!_checkedFolders.Contains(forCreateDirPart))
            {
                if (!_destinationFileManager.CareCreateDirectory(afterRootPath, dir, true))
                {
                    return null;
                }

                _checkedFolders.Add(forCreateDirPart);
            }

            afterRootPath = forCreateDirPart;
        }

        return afterRootPath;
    }

    //private List<MyFileInfo> GetFileInfos(string? afterRootPath)
    //{
    //    if (afterRootPath is null)
    //        return _destinationFileManager.GetFilesWithInfo(null, null);
    //    if (!_checkedFolderFiles.ContainsKey(afterRootPath))
    //        _checkedFolderFiles.Add(afterRootPath, _destinationFileManager.GetFilesWithInfo(afterRootPath, null));
    //    return _checkedFolderFiles[afterRootPath];
    //}
}
