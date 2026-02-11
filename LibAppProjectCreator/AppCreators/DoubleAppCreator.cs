using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator.FolderProcessors;
using LibAppProjectCreator.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.FileManagersMain;

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
        //ძირითადი აპის შემქმნელის შექმნა
        _mainAppCreator = CreateMainAppCreator();
        if (_mainAppCreator is null)
            return false;

        //შევამოწმოთ არსებობს თუ არა ძირითადი აპის სამუშაო ფოლდერი.
        var isWorkPathExists = Directory.Exists(_mainAppCreator.WorkPath);

        //თუ სამუშაო ფოლდერი არ არსებობს, მაშინ შევქმნათ სამუშაო ფოლდერი და შევქმნათ აპი.
        //თუ სამუშაო ფოლდერი არსებობს, მაშინ მხოლოდ სინქრონიზაცია გავაკეთოთ გიტის.
        if (!await _mainAppCreator.PrepareParametersAndCreateApp(true, cancellationToken,
                isWorkPathExists ? ECreateAppVersions.OnlySyncGit : ECreateAppVersions.DoAll))
            return false;

        //თუ სამუშაო ფოლდერი არ არსებობდა აპლიკაციის შექმნის პროცესის გაშვებამდე,
        //მაშინ აპის შეიქმნებოდა მთლიანად და ამიტომ პროცესი დასრულებულია
        if (!isWorkPathExists)
            return true;

        //შევქმნათ ფაილმენეჯერი ძირითადი აპის სამუშაო ფოლდერისთვის
        var mainWorkPathFileManager =
            FileManagersFactory.CreateFileManager(_useConsole, _logger, _mainAppCreator.WorkPath);

        if (mainWorkPathFileManager == null)
        {
            StShared.WriteErrorLine($"FileManager does not created for folder {_mainAppCreator.WorkPath}", _useConsole,
                _logger);
            return false;
        }

        //წავშალოთ ყველა ბინარული ფაილები და მისი შემცველი ფოლდერები,
        //რადგან ისინი არ გვჭირდება სინქრონიზაციისთვის და მხოლოდ პრობლემებს შექმნიან
        var deleteBinObjFolders = new DeleteBinObjFolders(mainWorkPathFileManager);
        deleteBinObjFolders.Run();

        //შევქმნათ დროებითი აპის შემქმნელი
        var tempAppCreator = CreateTempAppCreator();
        if (tempAppCreator is null)
            return false;

        //შევქმნათ დროებითი აპი და მოვამზადოთ მისი პარამეტრები
        if (!await tempAppCreator.PrepareParametersAndCreateApp(false, cancellationToken, ECreateAppVersions.Temp))
            return false;


        return SyncSolution(tempAppCreator.WorkPath, mainWorkPathFileManager);
    }

    private bool SyncSolution(string tempSolutionPath, FileManager mainSolutionFileManager)
    {
        //შევქმნათ ლოკალური გამგზავნი ფაილ მენეჯერი
        var sourceFileManager = FileManagersFactory.CreateFileManager(_useConsole, _logger, tempSolutionPath);

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
        var deleteRedundantFiles =
            new DeleteRedundantFiles(sourceFileManager, mainSolutionFileManager, excludeSet, excludeFolders);
        deleteRedundantFiles.Run();

        return true;
    }

    protected abstract AppCreatorBase? CreateMainAppCreator();
    protected abstract AppCreatorBase? CreateTempAppCreator();
}