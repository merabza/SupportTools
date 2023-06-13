using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class DeleteTaskCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    public DeleteTaskCommandCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliMenu",
            "using CliParameters",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new CodeBlock("public sealed class DeleteTaskCommand : CliMenuCommand",
                "private readonly ParametersManager _parametersManager",
                "private readonly string _taskName",
                new CodeBlock(
                    "public DeleteTaskCommand(ParametersManager parametersManager, string taskName) : base(\"Delete Task\",taskName)",
                    "_parametersManager = parametersManager",
                    "_taskName = taskName"),
                new CodeBlock("public override void Run()",
                    new CodeBlock("try",
                        $"{_projectNamespace}Parameters parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                        "TaskModel? task = parameters.GetTask(_taskName)",
                        new CodeBlock("if (task == null)",
                            "StShared.WriteErrorLine($\"Task { _taskName } does not found\", true)",
                            "return"),
                        new CodeBlock(
                            "if (!Inputer.InputBool($\"This will Delete  Task { _taskName }.are you sure ? \", false, false))",
                            "return"),
                        "parameters.RemoveTask(_taskName)",
                        "_parametersManager.Save(parameters, $\"Task { _taskName } deleted.\")",
                        "MenuAction = EMenuAction.LevelUp",
                        "return"),
                    new CodeBlock("catch (DataInputEscapeException)",
                        "Console.WriteLine()",
                        "Console.WriteLine(\"Escape... \")",
                        "StShared.Pause()"),
                    new CodeBlock("catch (Exception e)",
                        "StShared.WriteException(e, true)"),
                    "MenuAction = EMenuAction.Reload")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}