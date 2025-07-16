using System;
using CodeTools;
using Microsoft.Extensions.Logging;

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
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System", "using CliMenu", "using LibParameters", "using CliParameters.CliMenuCommands",
            $"using {(_useDatabase ? "Do" : string.Empty)}{_projectNamespace}.Models", "using LibDataInput",
            "using Microsoft.Extensions.Logging", string.Empty, $"namespace {_projectNamespace}.MenuCommands",
            string.Empty, new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
            new CodeBlock("public sealed class TaskSubMenuCommand : CliMenuCommand", "private readonly ILogger _logger",
                "private readonly ParametersManager _parametersManager", string.Empty,
                new CodeBlock(
                    "public TaskSubMenuCommand(ILogger logger, ParametersManager parametersManager, string taskName) : base(taskName, EMenuAction.LoadSubMenu)",
                    "_logger = logger", "_parametersManager = parametersManager"),
                new CodeBlock("protected override bool RunBody()", "return true"),
                new CodeBlock("public override CliMenuSet GetSubMenu()",
                    "CliMenuSet taskSubMenuSet = new($\" Task => { Name}\")",
                    "var deleteTaskCommand = new DeleteTaskCommand(_parametersManager, Name)",
                    "taskSubMenuSet.AddMenuItem(deleteTaskCommand)",
                    "taskSubMenuSet.AddMenuItem(new EditTaskNameCommand(_parametersManager, Name))",
                    "taskSubMenuSet.AddMenuItem(new TaskCommand(_logger, _parametersManager, Name))",
                    new OneLineComment(
                        "ეს საჭირო იქნება, თუ ამ მენიუში საჭირო გახდება ამოცანის დამატებითი რედაქტორების შექმნა"),
                    $"var parameters = ({_projectNamespace}Parameters)_parametersManager.Parameters",
                    "var task = parameters.GetTask(Name)", "var key = ConsoleKey.Escape.Value().ToLower()",
                    "taskSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand(\"Exit to level up menu\", null), key.Length)",
                    "return taskSubMenuSet")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}