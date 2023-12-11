using ConnectTools;
using FileManagersMain;
using LibFileParameters.Models;
using System.Collections.Generic;
using System.Linq;

namespace LibAppProjectCreator.FolderProcessors;

public class CopyAndReplaceFilesAndFolders : FolderProcessor
{
    private readonly Dictionary<string, List<MyFileInfo>> _checkedFolderFiles = new();
    private readonly List<string> _checkedFolders = new();

    private readonly FileManager _destinationFileManager;

    protected CopyAndReplaceFilesAndFolders(FileManager sourceFileManager, FileManager destinationFileManager) : base(
        "Copy And Replace files and folders",
        "First part of files synchronization Copy And Replace files from one place to another", sourceFileManager, null,
        false, new ExcludeSet { FolderFileMasks = new() { ".git", ".vs", ".gitignore" } })
    {
        _destinationFileManager = destinationFileManager;
    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        var dirNames = afterRootPath is null
            ? new List<string>()
            : afterRootPath.PrepareAfterRootPath(FileManager.DirectorySeparatorChar);
        var preparedDestinationAfterRootPath = CheckDestinationDirs(dirNames);


        var preparedFileName = file.FileName;

        var myFileInfo = GetOneFileWithInfo(preparedDestinationAfterRootPath, preparedFileName);

        //თუ ფაილის სახელი და სიგრძე ემთხვევა, ვთვლით, რომ იგივე ფაილია
        if (myFileInfo != null && myFileInfo.FileLength == file.FileLength)
            return true;
    }

    private string? CheckDestinationDirs(IEnumerable<string> dirNames)
    {
        string? afterRootPath = null;
        foreach (var dir in dirNames)
        {
            var forCreateDirPart = _destinationFileManager.PathCombine(afterRootPath, dir);
            if (!_checkedFolders.Contains(forCreateDirPart))
            {
                if (!_destinationFileManager.CareCreateDirectory(afterRootPath, dir, true))
                    return null;
                _checkedFolders.Add(forCreateDirPart);
            }

            afterRootPath = forCreateDirPart;
        }

        return afterRootPath;
    }

    private MyFileInfo? GetOneFileWithInfo(string? afterRootPath, string fileName)
    {
        return GetFileInfos(afterRootPath).SingleOrDefault(x => x.FileName == fileName);
    }

    private List<MyFileInfo> GetFileInfos(string? afterRootPath)
    {
        if (afterRootPath is null)
            return _destinationFileManager.GetFilesWithInfo(null, null);
        if (!_checkedFolderFiles.ContainsKey(afterRootPath))
            _checkedFolderFiles.Add(afterRootPath, _destinationFileManager.GetFilesWithInfo(afterRootPath, null));
        return _checkedFolderFiles[afterRootPath];
    }

}