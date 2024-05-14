using CodeTools;
using Microsoft.Extensions.Logging;
using System;

namespace LibAppProjectCreator.CodeCreators;

public sealed class EditTaskNameCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
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
            "using System",
            "using CliMenu",
            "using LibParameters",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new CodeBlock("public sealed class EditTaskNameCommand : CliMenuCommand",
                "private readonly ParametersManager _parametersManager",
                "private readonly string _taskName",
                new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
                new CodeBlock(
                    "public EditTaskNameCommand(ParametersManager parametersManager, string taskName) : base(\"Edit Task\",taskName)",
                    "_parametersManager = parametersManager",
                    "_taskName = taskName"),
                new CodeBlock("protected override void RunAction()",
                    $"var parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                    "var task = parameters.GetTask(_taskName)",
                    new CodeBlock("if (task == null)",
                        "StShared.WriteErrorLine($\"Task { _taskName } does not found\", true)",
                        "return"),
                    "",
                    new OneLineComment("ამოცანის სახელის რედაქტირება"),
                    "var newTaskName = Inputer.InputText(\"change  Task Name \", _taskName)",
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
                new CodeBlock("protected override string GetStatus()",
                    "return _taskName")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}