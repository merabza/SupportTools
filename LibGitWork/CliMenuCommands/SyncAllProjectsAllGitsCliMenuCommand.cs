//using System.Threading;
//using CliMenu;
//using LibGitWork.ToolActions;
//using LibParameters;
//using Microsoft.Extensions.Logging;

//namespace LibGitWork.CliMenuCommands;

//public sealed class SyncAllProjectsAllGitsCliMenuCommand : CliMenuCommand
//{
//    private readonly ILogger _logger;
//    private readonly ParametersManager _parametersManager;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public SyncAllProjectsAllGitsCliMenuCommand(ILogger logger, ParametersManager parametersManager) : base(
//        "Sync All Projects All Gits", EMenuAction.Reload, EMenuAction.Reload, null, true)
//    {
//        _logger = logger;
//        _parametersManager = parametersManager;
//    }

//    protected override bool RunBody()
//    {
//        var syncOneProjectAllGitsToolAction =
//            SyncMultipleProjectsGitsToolAction.Create(_logger, _parametersManager, null, null, true);
//        return syncOneProjectAllGitsToolAction.Run(CancellationToken.None).Result;
//    }
//}