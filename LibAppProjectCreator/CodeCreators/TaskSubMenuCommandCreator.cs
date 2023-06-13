using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class TaskSubMenuCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    public TaskSubMenuCommandCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
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
            "using CliParameters.MenuCommands",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using Microsoft.Extensions.Logging",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new CodeBlock("public sealed class TaskSubMenuCommand : CliMenuCommand",
                "private readonly ILogger _logger",
                "private readonly ParametersManager _parametersManager",
                "",
                new CodeBlock(
                    "public TaskSubMenuCommand(ILogger logger, ParametersManager parametersManager, string taskName) : base(taskName)",
                    "_logger = logger",
                    "_parametersManager = parametersManager"),
                new CodeBlock("public override void Run()",
                    "MenuAction = EMenuAction.LoadSubMenu"),
                new CodeBlock("public override CliMenuSet GetSubmenu()",
                    "CliMenuSet taskSubMenuSet = new($\" Task => { Name}\")",
                    new CodeBlock("if (Name is not null)",
                        "DeleteTaskCommand deleteTaskCommand = new(_parametersManager, Name)",
                        "taskSubMenuSet.AddMenuItem(deleteTaskCommand)",
                        "taskSubMenuSet.AddMenuItem(new EditTaskNameCommand(_parametersManager, Name), \"Edit  task Name\")",
                        "taskSubMenuSet.AddMenuItem(new TaskCommand(_logger, _parametersManager, Name), \"Run this task\")",
                        $"{_projectNamespace}Parameters parameters = ({_projectNamespace}Parameters)_parametersManager.Parameters",
                        "TaskModel ? task = parameters.GetTask(Name)"),
                    "string key = ConsoleKey.Escape.Value().ToLower()",
                    "taskSubMenuSet.AddMenuItem(key, \"Exit to Main menu\", new ExitToMainMenuCommand(null, null), key.Length)",
                    "return taskSubMenuSet")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}