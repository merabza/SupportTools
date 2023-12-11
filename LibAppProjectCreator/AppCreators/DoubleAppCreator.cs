using System.Collections.Generic;
using System.IO;
using FileManagersMain;
using LibAppProjectCreator.Models;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using SystemToolsShared;

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

    public List<GitCloneDataModel> GitClones => _mainAppCreator?.GitClones ?? new List<GitCloneDataModel>();

    public bool CreateDoubleApp()
    {
        _mainAppCreator = CreateMainAppCreator();
        if ( _mainAppCreator is null )
            return false;

        var solutionPathExists = Directory.Exists(_mainAppCreator.SolutionPath);

        if (!_mainAppCreator.PrepareParametersAndCreateApp(solutionPathExists
                ? ECreateAppVersions.OnlySyncGit
                : ECreateAppVersions.DoAll))
            return false;

        if ( ! solutionPathExists )
            return true;

        var tempAppCreator = CreateTempAppCreator();
        if ( tempAppCreator is null )
            return false;

        if (!tempAppCreator.PrepareParametersAndCreateApp(ECreateAppVersions.WithoutSolutionGitInit))
            return false;

        if ( ! SyncSolution(tempAppCreator.SolutionPath, _mainAppCreator.SolutionPath) )
            return false;
        

        return true;
    }

    private bool SyncSolution(string tempSolutionPath, string mainSolutionPath)
    {
        //შევქმნათ ლოკალური გამგზავნი ფაილ მენეჯერი
        var sourceFileManager = FileManagersFabric.CreateFileManager(_useConsole, _logger, tempSolutionPath);

        if (sourceFileManager == null)
        {
            StShared.WriteErrorLine($"sourceFileManager does not created for folder {tempSolutionPath}", _useConsole,
                _logger);
            return false;
        }

        //შევქმნათ ლოკალური მიმღები ფაილ მენეჯერი
        var destinationFileManager = FileManagersFabric.CreateFileManager(_useConsole, _logger, mainSolutionPath);

        if (destinationFileManager == null)
        {
            StShared.WriteErrorLine($"sourceFileManager does not created for folder {mainSolutionPath}", _useConsole,
                _logger);
            return false;
        }


        return true;
    }

    protected abstract AppCreatorBase? CreateMainAppCreator();
    protected abstract AppCreatorBase? CreateTempAppCreator();
}