//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;
//using CliParameters;
//using LibAppProjectCreator;
//using LibDotnetWork;
//using LibParameters;
//using LibScaffoldSeeder.Models;
//using Microsoft.Extensions.Logging;

//namespace LibScaffoldSeeder.ToolCommands;

//public sealed class JsonFromProjectDbProjectGetter : ToolCommand
//{
//    private const string ActionName = "Get Json From Project DbProject";
//    private const string ActionDescription = "Get Json From Project DbProject";
//    private readonly ILogger _logger;
//    private readonly JsonFromProjectDbProjectGetterParameters _parameters;

//    //პარამეტრები მოეწოდება პირდაპირ კონსტრუქტორში
//    // ReSharper disable once ConvertToPrimaryConstructor
//    public JsonFromProjectDbProjectGetter(ILogger logger, JsonFromProjectDbProjectGetterParameters parameters,
//        IParametersManager parametersManager) : base(logger, ActionName, parameters, parametersManager,
//        ActionDescription)
//    {
//        _logger = logger;
//        _parameters = parameters;
//    }

//    //private JsonFromProjectDbProjectGetterParameters CorrectNewDbParameters =>
//    //    (JsonFromProjectDbProjectGetterParameters)Par;

//    protected override bool CheckValidate()
//    {
//        //if (string.IsNullOrWhiteSpace(CorrectNewDbParameters.GetJsonFromScaffoldDbProjectFileFullName))
//        //{
//        //    _logger.LogError("GetJsonFromScaffoldDbProjectFileFullName not specified");
//        //    return false;
//        //}

//        //if (!string.IsNullOrWhiteSpace(CorrectNewDbParameters.GetJsonFromScaffoldDbProjectParametersFileFullName))
//        //    return true;

//        //_logger.LogError("GetJsonFromScaffoldDbProjectParametersFileFullName not specified");
//        //return false;

//        return true;
//    }

//    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
//    {
//        var getJsonFromScaffoldDbProjectName =
//            NamingStats.GetJsonFromScaffoldDbProjectName(_parameters.DbContextProjectName);

//        var getJsonFromScaffoldDbProjectFilePath = Path.Combine(_parameters.ScaffoldSeedersWorkFolder,
//            _parameters.ProjectName, _parameters.ScaffoldSeederProjectName, _parameters.ScaffoldSeederProjectName,
//            getJsonFromScaffoldDbProjectName, $"{getJsonFromScaffoldDbProjectName}{NamingStats.CsProjectExtension}");

//        var dotnetProcessor = new DotnetProcessor(_logger, true);
//        return ValueTask.FromResult(dotnetProcessor
//            .RunToolUsingParametersFile(getJsonFromScaffoldDbProjectFilePath, getJsonFromScaffoldDbProjectFilePath)
//            .IsNone);
//    }
//}