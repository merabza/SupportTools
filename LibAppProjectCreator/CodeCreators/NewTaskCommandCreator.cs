using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class NewTaskCommandCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

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
            "using CliMenu",
            "using CliParameters",
            $"using {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "using LibDataInput",
            "using SystemToolsShared",
            "",
            $"namespace {_projectNamespace}.MenuCommands",
            "",
            new CodeBlock("public sealed class NewTaskCommand : CliMenuCommand",
                "private readonly ParametersManager _parametersManager",
                "",
                new CodeBlock("public NewTaskCommand(ParametersManager parametersManager) : base(\"New Task\")",
                    "_parametersManager = parametersManager"),
                new CodeBlock("public override void Run()",
                    "MenuAction = EMenuAction.Reload",
                    new CodeBlock("try",
                        $"{_projectNamespace}Parameters parameters = ({_projectNamespace}Parameters) _parametersManager.Parameters",
                        "",
                        new OneLineComment("ამოცანის შექმნის პროცესი დაიწყო"),
                        "Console.WriteLine(\"Create new Task started\")",
                        "",
                        "string? newTaskName = Inputer.InputText(\"New Task Name\", null)",
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
                        ""),
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