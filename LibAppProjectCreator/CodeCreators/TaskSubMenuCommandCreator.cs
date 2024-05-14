using CodeTools;
using Microsoft.Extensions.Logging;
using System;

namespace LibAppProjectCreator.CodeCreators;

public sealed class TaskSubMenuCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
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
            "using System",
            "using CliMenu",
            "using LibParameters",
            "using CliParameters.CliMenuCommands",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using Microsoft.Extensions.Logging",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
            new CodeBlock("public sealed class TaskSubMenuCommand : CliMenuCommand",
                "private readonly ILogger _logger",
                "private readonly ParametersManager _parametersManager",
                "",
                new CodeBlock(
                    "public TaskSubMenuCommand(ILogger logger, ParametersManager parametersManager, string taskName) : base(taskName)",
                    "_logger = logger",
                    "_parametersManager = parametersManager"),
                new CodeBlock("protected override void RunAction()",
                    "MenuAction = EMenuAction.LoadSubMenu"),
                new CodeBlock("public override CliMenuSet GetSubmenu()",
                    "CliMenuSet taskSubMenuSet = new($\" Task => { Name}\")",
                    new CodeBlock("if (Name is not null)",
                        "var deleteTaskCommand = new DeleteTaskCommand(_parametersManager, Name)",
                        "taskSubMenuSet.AddMenuItem(deleteTaskCommand)",
                        "taskSubMenuSet.AddMenuItem(new EditTaskNameCommand(_parametersManager, Name), \"Edit  task Name\")",
                        "taskSubMenuSet.AddMenuItem(new TaskCommand(_logger, _parametersManager, Name), \"Run this task\")",
                        new OneLineComment(
                            "ეს საჭირო იქნება, თუ ამ მენიუში საჭირო გახდება ამოცანის დამატებითი რედაქტორების შექმნა"),
                        $"var parameters = ({_projectNamespace}Parameters)_parametersManager.Parameters",
                        "var task = parameters.GetTask(Name)"
                    ),
                    "var key = ConsoleKey.Escape.Value().ToLower()",
                    "taskSubMenuSet.AddMenuItem(key, \"Exit to Main menu\", new ExitToMainMenuCliMenuCommand(null, null), key.Length)",
                    "return taskSubMenuSet")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}