using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class TaskCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

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
            "using System.Diagnostics",
            "using CliMenu",
            "using CliParameters",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using Microsoft.Extensions.Logging",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
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
                new CodeBlock("public override void Run()",
                    new CodeBlock("try",
                        "MenuAction = EMenuAction.Reload",
                        $"{_projectNamespace}Parameters parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                        "TaskModel? task = parameters.GetTask(_taskName)",
                        new CodeBlock("if (task == null)",
                            "StShared.WriteErrorLine($\"Task { _taskName } does not found\", true)",
                            "return"),
                        "",
                        $"{_projectNamespace}TaskRunner crawlerRunner = new(_logger, parameters, _taskName, task)",
                        "",
                        new OneLineComment("დავინიშნოთ დრო"),
                        "Stopwatch watch = Stopwatch.StartNew()",
                        "Console.WriteLine(\"Crawler is running...\")",
                        "Console.WriteLine(\"-- - \")",
                        " crawlerRunner.Run()",
                        "watch.Stop()",
                        "Console.WriteLine(\"-- - \")",
                        "Console.WriteLine($\"Crawler Finished.Time taken: { watch.Elapsed.Seconds } second(s)\")",
                        "StShared.Pause()"),
                    new CodeBlock("catch (DataInputEscapeException)",
                        "Console.WriteLine()",
                        "Console.WriteLine(\"Escape... \")",
                        "StShared.Pause()"),
                    new CodeBlock("catch (Exception e)",
                        "StShared.WriteException(e, true)"))
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}