using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class TestModelClassCreator : CodeCreator
{
    private readonly string _projectNamespace;
    private readonly bool _useCarcass;

    public TestModelClassCreator(ILogger logger, string placePath, string projectNamespace, bool useCarcass,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useCarcass = useCarcass;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            _useCarcass ? null : "using CliParameters",
            "",
            $"namespace {_projectNamespace}Db.Models",
            "",
            new CodeBlock($"public sealed class TestModel{(_useCarcass ? "" : " : ItemData")}",
                new CodeBlock("public int TestId", true, "get", "set"),
                new CodeBlock("public string TestName", true, "get", "set"),
                new CodeBlock("public TestModel(string testName)",
                    "TestName = testName")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}