using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators.Database;

public sealed class TestModelClassCreator : CodeCreator
{
    private readonly bool _isApi;
    private readonly string _projectNamespace;
    private readonly bool _useCarcass;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TestModelClassCreator(ILogger logger, string placePath, string projectNamespace, bool useCarcass, bool isApi,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
        _useCarcass = useCarcass;
        _isApi = isApi;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            _useCarcass || _isApi ? null : "using LibParameters", string.Empty,
            $"namespace {_projectNamespace}Db.Models", string.Empty,
            new OneLineComment(
                "ეს არის სატესტო მოდელი, რომელიც არის უბრალოდ ნიმუშისათვის და შესაძლებელია წაიშალოს საჭირების შემთხვევაში"),
            string.Empty,
            new CodeBlock($"public sealed class TestModel{(_useCarcass || _isApi ? "" : " : ItemData")}",
                new CodeBlock("public int TestId", true, "get", "set"),
                new CodeBlock("public string TestName", true, "get", "set"), string.Empty,
                new OneLineComment(" ReSharper disable once ConvertToPrimaryConstructor"),
                new CodeBlock("public TestModel(string testName)", "TestName = testName")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}