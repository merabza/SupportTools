using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileManagersMain;
using LibAppProjectCreator.FolderProcessors;
using LibAppProjectCreator.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.AppCreators;

public abstract class DoubleAppCreator
{
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    private AppCreatorBase? _mainAppCreator;

    protected DoubleAppCreator(ILogger logger, bool useConsole)
    {
        _logger = logger;
        _useConsole = useConsole;
    }

    public List<GitCloneDataModel> GitClones => _mainAppCreator?.GitClones ?? [];

    public async Task<bool> CreateDoubleApp(CancellationToken cancellationToken = default)
    {
        _mainAppCreator = CreateMainAppCreator();
        if (_mainAppCreator is null)
            return false;

        var solutionPathExists = Directory.Exists(_mainAppCreator.SolutionPath);

        if (!await _mainAppCreator.PrepareParametersAndCreateApp(true, cancellationToken,
                solutionPathExists ? ECreateAppVersions.OnlySyncGit : ECreateAppVersions.DoAll))
            return false;

        if (!solutionPathExists)
            return true;

        var mainSolutionFileManager =
            FileManagersFabric.CreateFileManager(_useConsole, _logger, _mainAppCreator.SolutionPath);

        if (mainSolutionFileManager == null)
        {
            StShared.WriteErrorLine($"sourceFileManager does not created for folder {_mainAppCreator.SolutionPath}",
                _useConsole, _logger);
            return false;
        }

        var deleteBinObjFolders = new DeleteBinObjFolders(mainSolutionFileManager);
        deleteBinObjFolders.Run();

        var tempAppCreator = CreateTempAppCreator();
        if (tempAppCreator is null)
            return false;

        if (!await tempAppCreator.PrepareParametersAndCreateApp(true, cancellationToken,
                ECreateAppVersions.Temp))
            return false;

        return SyncSolution(tempAppCreator.SolutionPath, mainSolutionFileManager);
    }

    private bool SyncSolution(string tempSolutionPath, FileManager mainSolutionFileManager)
    {
        //შევქმნათ ლოკალური გამგზავნი ფაილ მენეჯერი
        var sourceFileManager = FileManagersFabric.CreateFileManager(_useConsole, _logger, tempSolutionPath);

        if (sourceFileManager == null)
        {
            StShared.WriteErrorLine($"sourceFileManager does not created for folder {tempSolutionPath}", _useConsole,
                _logger);
            return false;
        }

        var excludeSet = new ExcludeSet { FolderFileMasks = [@"*\.git\*", @"*\.vs\*", @"*\.gitignore", @"*\obj\*"] };

        if (!sourceFileManager.IsFolderEmpty(null))
        {
            var copyAndReplaceFilesAndFolders =
                new CopyAndReplaceFilesAndFolders(sourceFileManager, mainSolutionFileManager, excludeSet);
            copyAndReplaceFilesAndFolders.Run();
        }

        if (mainSolutionFileManager.IsFolderEmpty(null))
            return true;

        var excludeFolders = new[] { ".git", ".vs", "obj" };
        DeleteRedundantFiles deleteRedundantFiles =
            new(sourceFileManager, mainSolutionFileManager, excludeSet, excludeFolders);
        deleteRedundantFiles.Run();

        return true;
    }

    protected abstract AppCreatorBase? CreateMainAppCreator();
    protected abstract AppCreatorBase? CreateTempAppCreator();
}