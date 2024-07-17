using CodeTools;
using Microsoft.Extensions.Logging;
using System;

namespace LibAppProjectCreator.CodeCreators;

internal class AssemblyReferenceClassCreator : CodeCreator
{
    private readonly string _projectNamespace;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AssemblyReferenceClassCreator(ILogger logger, string placePath, string projectNamespace,
        string? codeFileName = null) : base(logger, placePath, codeFileName)
    {
        _projectNamespace = projectNamespace;
    }

    public override void CreateFileStructure()
    {
        var block = new CodeBlock(string.Empty,
            new OneLineComment($"Created by {GetType().Name} at {DateTime.Now}"),
            string.Empty,
            "using System.Reflection",
            string.Empty,
            $"namespace {_projectNamespace}",
            string.Empty,
            new CodeBlock("public static class AssemblyReference",
                "public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly"),
            string.Empty);
        CodeFile.AddRange(block.CodeItems);
        FinishAndSave();
    }
}