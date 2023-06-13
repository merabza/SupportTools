﻿using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.CarcassAndDatabase;

public sealed class TestQueryClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    public TestQueryClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock("",
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "",
            $"namespace {_projectNamespace}Db.QueryModels",
            "",
            new CodeBlock("public sealed class TestQuery",
                new CodeBlock("public int TestId", true, "get", "set"),
                new CodeBlock("public string TestName", true, "get", "set"),
                new CodeBlock("public TestQuery(string testName)",
                    "TestName = testName")
            ));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}