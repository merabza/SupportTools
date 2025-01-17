using System;
using CodeTools;
using Microsoft.Extensions.Logging;

namespace LibAppProjectCreator.CodeCreators;

public sealed class CreatorClassCreator : CodeCreator
{
    private readonly string _creatorClassName;
    private readonly CodeBlock _generatedCodeBlock;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreatorClassCreator(ILogger logger, string placePath, string creatorClassName, CodeBlock generatedCodeBlock,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _creatorClassName = creatorClassName;
        _generatedCodeBlock = generatedCodeBlock;
    }


    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty, new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            "using System", "using CodeTools", "using CodeTools.CodeCreators", "using Microsoft.Extensions.Logging",
            string.Empty, "namespace LibAppProjectCreator.CodeCreators",
            new CodeBlock($"public sealed class {_creatorClassName} : CodeCreator",
                "private readonly string _projectNamespace", string.Empty,
                new CodeBlock(
                    $"public {_creatorClassName}(ILogger logger, string placePath, string projectNamespace, string? codeFileName = null) : base(logger, placePath, codeFileName)",
                    "_projectNamespace = projectNamespace"),
                new CodeBlock("public override void CreateFileStructure()",
                    "CodeBlock block = " + _generatedCodeBlock.OutputCreator(0, 2),
                    "CodeFile.AddRange(block.CodeItems)", "FinishAndSave()")));
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}