using System;
using AppCliTools.CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class EditTaskNameCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditTaskNameCommandCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System", "using CliMenu", "using LibParameters",
            $"using {(_useDatabase ? "Do" : string.Empty)}{_projectNamespace}.Models", "using LibDataInput",
            "using SystemTools.SystemToolsShared", string.Empty, $"namespace {_projectNamespace}.MenuCommands",
            string.Empty,
            new CodeBlock("public sealed class EditTaskNameCommand : CliMenuCommand",
                "private readonly ParametersManager _parametersManager", "private readonly string _taskName",
                new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
                new CodeBlock(
                    "public EditTaskNameCommand(ParametersManager parametersManager, string taskName) : base(\"Edit Task\", EMenuAction.LevelUp)",
                    "_parametersManager = parametersManager", "_taskName = taskName"),
                new CodeBlock("protected override bool RunBody()",
                    $"var parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                    "var task = parameters.GetTask(_taskName)",
                    new CodeBlock("if (task == null)",
                        "StShared.WriteErrorLine($\"Task { _taskName } does not found\", true)", "return false"),
                    string.Empty, new OneLineComment("ამოცანის სახელის რედაქტირება"),
                    "var newTaskName = Inputer.InputText(\"change  Task Name \", _taskName)",
                    new CodeBlock("if (string.IsNullOrWhiteSpace(newTaskName))", "return false"), string.Empty,
                    new CodeBlock("if (_taskName == newTaskName)", "return false"), string.Empty,
                    new CodeBlock("if (!parameters.RemoveTask(_taskName))",
                        "StShared.WriteErrorLine($\"Cannot change  Task with name { _taskName } to { newTaskName }, because cannot remove this  task\", true)",
                        "return false"), string.Empty,
                    new CodeBlock("if (!parameters.AddTask(newTaskName, task))",
                        "StShared.WriteErrorLine($\"Cannot change  Task with name { _taskName } to { newTaskName }, because cannot add this  task\", true)",
                        "return false"), string.Empty,
                    "_parametersManager.Save(parameters, $\" Task Renamed from { _taskName } To { newTaskName }\")",
                    string.Empty, "return true"),
                new CodeBlock("protected override string GetStatus()", "return _taskName")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}