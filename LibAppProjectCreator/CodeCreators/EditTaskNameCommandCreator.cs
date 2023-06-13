using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class EditTaskNameCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    public EditTaskNameCommandCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
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
            "using CliMenu",
            "using CliParameters",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new CodeBlock("public sealed class EditTaskNameCommand : CliMenuCommand",
                "private readonly ParametersManager _parametersManager",
                "private readonly string _taskName",
                new CodeBlock(
                    "public EditTaskNameCommand(ParametersManager parametersManager, string taskName) : base(\"Edit Task\",taskName)",
                    "_parametersManager = parametersManager",
                    "_taskName = taskName"),
                new CodeBlock("public override void Run()",
                    new CodeBlock("try",
                        $"{_projectNamespace}Parameters parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                        "TaskModel? task = parameters.GetTask(_taskName)",
                        new CodeBlock("if (task == null)",
                            "StShared.WriteErrorLine($\"Task { _taskName } does not found\", true)",
                            "return"),
                        "",
                        new OneLineComment("ამოცანის სახელის რედაქტირება"),
                        "string? newTaskName = Inputer.InputText(\"change  Task Name \", _taskName)",
                        new CodeBlock("if (string.IsNullOrWhiteSpace(newTaskName))",
                            "return"),
                        "",
                        new CodeBlock("if (_taskName == newTaskName)",
                            "return"),
                        "",
                        new CodeBlock("if (!parameters.RemoveTask(_taskName))",
                            "StShared.WriteErrorLine($\"Cannot change  Task with name { _taskName } to { newTaskName }, because cannot remove this  task\", true)",
                            "return"),
                        "",
                        new CodeBlock("if (!parameters.AddTask(newTaskName, task))",
                            "StShared.WriteErrorLine($\"Cannot change  Task with name { _taskName } to { newTaskName }, because cannot add this  task\", true)",
                            "return"),
                        "",
                        "_parametersManager.Save(parameters, $\" Task Renamed from { _taskName } To { newTaskName }\")",
                        "",
                        "MenuAction = EMenuAction.LevelUp",
                        "return"),
                    new CodeBlock("catch (DataInputEscapeException)",
                        "Console.WriteLine()",
                        "Console.WriteLine(\"Escape... \")",
                        "StShared.Pause()"),
                    new CodeBlock("catch (Exception e)",
                        "StShared.WriteException(e, true)"),
                    "MenuAction = EMenuAction.Reload"),
                new CodeBlock("protected override string GetStatus()",
                    "return _taskName")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}