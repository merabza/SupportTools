//using LibParameters;
//using LibToolActions;
//using Microsoft.Extensions.Logging;
//using SupportToolsData.Models;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//// ReSharper disable ConvertToPrimaryConstructor

//namespace LibTools.ToolActions;

//public sealed class ClearOneSolutionAllProjectsToolAction : ToolAction
//{
//    //private readonly ILogger? _logger;
//    //private readonly string _projectName;

//    public ClearOneSolutionAllProjectsToolAction(ILogger? logger, string projectName) : base(logger,
//        "Sync One Project All Gits",
//        null, null)
//    {
//        //_logger = logger;
//        //_projectName = projectName;
//    }

//    public static ClearOneSolutionAllProjectsToolAction Create(ILogger? logger, ParametersManager parametersManager,
//        string projectName)
//    {
//        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
//        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;

//        return new ClearOneSolutionAllProjectsToolAction(loggerOrNull, projectName);
//    }

//    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
//    {

//        return true;
//    }
//}

