using System.Linq;
using ConnectTools;
using FileManagersMain;
using LibFileParameters.Models;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.FolderProcessors;

public sealed class DeleteBinObjFolders : FolderProcessor
{
    private readonly string[] _deleteFolderNames = { "bin", "obj" };
    private readonly string[] _excludeFolders = { ".git", ".vs" };


    public DeleteBinObjFolders(FileManager fileManager) : base("Delete bin obj folders", "Delete bin obj folders",
        fileManager, null, false, new ExcludeSet(), true, false)
    {

    }

    protected override (bool, bool, bool) ProcessOneFolder(string? afterRootPath, string folderName)
    {
        if (_excludeFolders.Length > 0 && _excludeFolders.Contains(folderName))
            return (true, false, false);

        if (!_deleteFolderNames.Contains(folderName))
            return (true, false, true);

        var deleted = FileManager.DeleteDirectory(afterRootPath, folderName, true);
        return deleted ? (true, true, true) : (false, false, true);

    }

    protected override bool ProcessOneFile(string? afterRootPath, MyFileInfo file)
    {
        return true;
    }
}