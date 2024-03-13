using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class ProjectParametersClassCreator : CodeCreator
{
    private readonly string _inNamespace;

    private readonly string _projectNames;
    private readonly bool _useDatabase;
    private readonly bool _useMenu;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectParametersClassCreator(ILogger logger, string placePath, string projectNames, string inNamespace,
        bool useDatabase, bool useMenu, string? codeFileName = null) : base(logger, placePath,
        codeFileName)
    {
        _projectNames = projectNames;
        _inNamespace = inNamespace;
        _useDatabase = useDatabase;
        _useMenu = useMenu;
    }


    public override void CreateFileStructure()
    {
        var propertiesBlock = new FlatCodeBlock();
        var tasksBlock = new FlatCodeBlock();
        if (_useDatabase)
            propertiesBlock.Add(new CodeBlock("public DatabaseConnectionParameters? DatabaseConnectionParameters", true,
                "get", "set"));

        if (_useMenu)
        {
            propertiesBlock.Add(new CodeBlock("public Dictionary<string, TaskModel> Tasks", "= []", true, "get",
                "set"));
            tasksBlock.Add(new CodeBlock("public TaskModel? GetTask(string taskName)",
                "return Tasks.GetValueOrDefault(taskName)"));
            tasksBlock.Add(new CodeBlock("public bool CheckNewTaskNameValid(string oldTaskName, string newTaskName)",
                new CodeBlock("if (oldTaskName == newTaskName)",
                    "return true"),
                new CodeBlock("if (!Tasks.ContainsKey(oldTaskName))",
                    "return false"),
                "return !Tasks.ContainsKey(newTaskName)"));
            tasksBlock.Add(new CodeBlock("public bool RemoveTask(string taskName)",
                new CodeBlock("if (!Tasks.ContainsKey(taskName))",
                    "return false"),
                "Tasks.Remove(taskName)",
                "return true"));
            tasksBlock.Add(new CodeBlock("public bool AddTask(string newTaskName, TaskModel task)",
                "return Tasks.TryAdd(newTaskName, task)"));
        }

        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using LibParameters",
            _useMenu ? new CodeCommand("using System.Collections.Generic") : new CodeExtraLine(),
            _useDatabase ? new CodeCommand("using CliParametersDataEdit.Models") : new CodeExtraLine(),
            "",
            $"namespace {_inNamespace}.Models",
            "",
            new CodeBlock(
                $"public sealed class {_projectNames}Parameters : IParameters",
                new CodeBlock("public string? LogFolder", true, "get", "set"),
                propertiesBlock,
                new CodeBlock("public bool CheckBeforeSave()",
                    "return true"
                ),
                _useMenu ? tasksBlock : null));

        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}