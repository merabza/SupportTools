using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class TaskCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TaskCommandCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger,
        placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System",
            "using System.Diagnostics",
            "using CliMenu",
            "using LibParameters",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using Microsoft.Extensions.Logging",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
            new CodeBlock("public sealed class TaskCommand : CliMenuCommand",
                "private readonly ILogger _logger",
                "private readonly IParametersManager _parametersManager",
                "private readonly string _taskName",
                "",
                new CodeBlock(
                    "public TaskCommand(ILogger logger, IParametersManager parametersManager, string taskName)",
                    "_logger = logger",
                    "_parametersManager = parametersManager",
                    "_taskName = taskName"),
                new CodeBlock("protected override void RunAction()",
                    "MenuAction = EMenuAction.Reload",
                    $"var parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                    "var task = parameters.GetTask(_taskName)",
                    new CodeBlock("if (task == null)",
                        "StShared.WriteErrorLine($\"Task { _taskName } does not found\", true)",
                        "return"),
                    "",
                    $"{_projectNamespace}TaskRunner crawlerRunner = new(_logger, parameters, _taskName, task)",
                    "",
                    new OneLineComment("დავინიშნოთ დრო"),
                    "var watch = Stopwatch.StartNew()",
                    "Console.WriteLine(\"Crawler is running...\")",
                    "Console.WriteLine(\"-- - \")",
                    " crawlerRunner.Run()",
                    "watch.Stop()",
                    "Console.WriteLine(\"-- - \")",
                    "Console.WriteLine($\"Crawler Finished.Time taken: { watch.Elapsed.Seconds } second(s)\")",
                    "StShared.Pause()")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}