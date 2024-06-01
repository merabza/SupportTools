using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class NewTaskCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NewTaskCommandCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
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
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
            new CodeBlock("public sealed class NewTaskCommand : CliMenuCommand",
                "private readonly ParametersManager _parametersManager",
                "",
                new CodeBlock("public NewTaskCommand(ParametersManager parametersManager) : base(\"New Task\")",
                    "_parametersManager = parametersManager"),
                new CodeBlock("protected override void RunAction()",
                    "MenuAction = EMenuAction.Reload",
                    $"var parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                    "",
                    new OneLineComment("ამოცანის შექმნის პროცესი დაიწყო"),
                    "Console.WriteLine(\"Create new Task started\")",
                    "",
                    "var newTaskName = Inputer.InputText(\"New Task Name\", null)",
                    new CodeBlock("if (string.IsNullOrEmpty(newTaskName))",
                        "return"),
                    "",
                    new OneLineComment("ახალი ამოცანის შექმნა და ჩამატება ამოცანების სიაში"),
                    new CodeBlock("if (!parameters.AddTask(newTaskName, new TaskModel()))",
                        "StShared.WriteErrorLine($\"Task with Name { newTaskName } does not created\", true)",
                        "return"),
                    "",
                    new OneLineComment("პარამეტრების შენახვა (ცვლილებების გათვალისწინებით)"),
                    "_parametersManager.Save(parameters, \"Create New Task Finished\")",
                    "")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}