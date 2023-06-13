using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class TaskModelCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useDatabase;

    public TaskModelCreator(ILogger logger, string placePath, string projectNamespace, bool useDatabase,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useDatabase = useDatabase;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using CliParameters",
            "",
            $"namespace {(_useDatabase ? "Do" : "")}{_projectNamespace}.Models",
            "",
            new CodeBlock("public sealed class TaskModel : ItemData"));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}